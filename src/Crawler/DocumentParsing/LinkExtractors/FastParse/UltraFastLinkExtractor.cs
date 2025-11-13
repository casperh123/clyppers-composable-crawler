using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace BrokenLinkChecker.DocumentParsing.ModularLinkExtraction.FastParse;

public static class UltraFastLinkExtractor
{
     // Maximum length of a URL that will be extracted, also used for buffer overlap
    // to ensure URLs spanning buffer boundaries are handled correctly
    private const int BoundaryOverlap = 2048;

    // Main buffer size for reading from stream (512KB)
    // Chosen to balance between memory usage and read efficiency
    private const int BufferSize = 1024 * 256;

    // URL storage buffer sized as power of 2 for better memory alignment
    private const int UrlBufferSize = 32 * 1024;
    private const int MaxUrlLength = 2048;
    
    // 'href' stored in reverse order for little-endian comparison
    private const uint HrefPattern = 0x66657268;

    // Shared buffer pool to avoid repeated large allocations
    private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;

    // Pre-computed lookup tables for fast validation
    private static readonly byte[] ValidUrlChars = CreateValidUrlChars();
    private static readonly int[] HexLookup = CreateHexLookup();
    
    // SIMD constants for case-insensitive 'h' matching
    private static readonly Vector256<byte> LowerMask = Vector256.Create((byte)0x20);
    private static readonly Vector256<byte> HChar = Vector256.Create((byte)'h');

    // Thread-local buffers to avoid allocation and contention in multi-threaded scenarios
    private static readonly ThreadLocal<byte[]> UrlBuffer = new(() => new byte[UrlBufferSize]);
    private static readonly ThreadLocal<int[]> UrlLengths = new(() => new int[UrlBufferSize / MaxUrlLength]);

    // Creates lookup table for valid URL characters
    // Includes alphanumeric and special characters allowed in URLs
    private static byte[] CreateValidUrlChars()
    {
        var valid = new byte[128];
        for (int i = '0'; i <= '9'; i++) valid[i] = 1;
        for (int i = 'A'; i <= 'Z'; i++) valid[i] = 1;
        for (int i = 'a'; i <= 'z'; i++) valid[i] = 1;
        string special = "-._~:/?#[]@!$&'()*+,;=%";
        foreach (char c in special) valid[c] = 1;
        return valid;
    }

    // Creates lookup table for hex value parsing (%xx sequences)
    private static int[] CreateHexLookup()
    {
        var lookup = new int[128];
        for (int i = 0; i < lookup.Length; i++) lookup[i] = -1;
        for (int i = '0'; i <= '9'; i++) lookup[i] = i - '0';
        for (int i = 'a'; i <= 'f'; i++) lookup[i] = 10 + (i - 'a');
        for (int i = 'A'; i <= 'F'; i++) lookup[i] = 10 + (i - 'A');
        return lookup;
    }

    // Main entry point: Extracts href URLs from a stream
    public static async ValueTask<List<string>> ExtractHrefsAsync(
        Stream responseStream,
        CancellationToken cancellationToken = default)
    {
        // Initial capacity based on typical HTML page
        var foundLinks = new List<string>(1028);
        byte[] buffer = BufferPool.Rent(BufferSize + BoundaryOverlap);

        try
        {
            // Track bytes that need to be carried over to next read
            int remainingBytes = 0;
            Memory<byte> bufferMemory = buffer.AsMemory();

            // Get thread-local buffers
            byte[] urlBuffer = UrlBuffer.Value;
            int[] urlLengths = UrlLengths.Value;
            int urlCount = 0;

            // Main processing loop
            while (true)
            {
                // Read next chunk of data
                int bytesRead = await responseStream.ReadAsync(
                    bufferMemory.Slice(remainingBytes, BufferSize),
                    cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    // Process any final data
                    if (remainingBytes > 0)
                    {
                        ProcessBufferOptimized(
                            bufferMemory.Slice(0, remainingBytes).Span,
                            ref remainingBytes,
                            urlBuffer,
                            urlLengths,
                            ref urlCount,
                            isLastBuffer: true);

                        if (urlCount > 0)
                        {
                            UrlCreater.ConvertUrlsToStrings(urlBuffer, urlLengths, urlCount, MaxUrlLength, foundLinks);
                        }
                    }
                    break;
                }

                // Process current buffer
                int totalBytes = remainingBytes + bytesRead;
                ProcessBufferOptimized(
                    bufferMemory.Slice(0, totalBytes).Span,
                    ref remainingBytes,
                    urlBuffer,
                    urlLengths,
                    ref urlCount,
                    isLastBuffer: false);

                // Copy remaining bytes to start of buffer if needed
                if (remainingBytes > 0 && remainingBytes < totalBytes)
                {
                    bufferMemory.Slice(totalBytes - remainingBytes, remainingBytes)
                        .CopyTo(bufferMemory);
                }

                // Convert found URLs to strings
                if (urlCount > 0)
                {
                    UrlCreater.ConvertUrlsToStrings(urlBuffer, urlLengths, urlCount, MaxUrlLength, foundLinks);
                    urlCount = 0;
                }
            }

            return foundLinks;
        }
        finally
        {
            BufferPool.Return(buffer);
        }
    }

    // Core processing method: finds and validates URLs in the current buffer
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ProcessBufferOptimized(
        Span<byte> buffer,
        ref int remainingBytes,
        byte[] urlBuffer,
        int[] urlLengths,
        ref int urlCount,
        bool isLastBuffer)
    {
        fixed (byte* bufPtr = buffer)
        fixed (byte* urlBufPtr = urlBuffer)
        {
            // Setup buffer boundaries and safety margins
            int position = 0;
            int length = buffer.Length;
            int safeLength = length - 4;  // Leave room for 'href' pattern matching
            byte* endPtr = bufPtr + length;

            // Process in chunks to optimize CPU cache usage
            const int CHUNK_SIZE = 4096;  // Typical CPU cache line multiple
            byte* chunkEnd = bufPtr + Math.Min(CHUNK_SIZE, safeLength);

            while (position < safeLength)
            {
                // Prefetch next chunk for better performance
                if (position + CHUNK_SIZE < safeLength)
                {
                    Sse.Prefetch0(bufPtr + position + CHUNK_SIZE);
                }

                // Find next occurrence of 'href'
                int hrefPos = FindNextHrefOptimized(bufPtr + position, length - position);
                if (hrefPos == -1)
                {
                    // Keep minimal bytes for next buffer to handle split patterns
                    remainingBytes = isLastBuffer ? 0 : Math.Min(MaxUrlLength, length - position);
                    return;
                }

                position += hrefPos;
                byte* currentPtr = bufPtr + position;
            
                // Find and validate quoted URL after href
                var quotePos = QuoteFinder.FindQuoteOptimized(currentPtr, (int)(endPtr - currentPtr));
                if (!quotePos.IsValid)
                {
                    // Handle potential split quote at buffer boundary
                    if (!isLastBuffer && position + 4 >= safeLength)
                    {
                        remainingBytes = length - position;
                        return;
                    }
                    position += 4;
                    continue;
                }

                // Extract URL between quotes
                int urlStart = quotePos.Start + 1;
                int urlLength = quotePos.Length;
            
                // Validate URL if it's within size limits
                if (urlLength > 0 && urlLength < MaxUrlLength)
                {
                    byte* urlStartPtr = currentPtr + urlStart;
                
                    // Quick validation of first few characters
                    bool quickValid = true;
                    for (int i = 0; i < Math.Min(8, urlLength); i++)
                    {
                        byte c = urlStartPtr[i];
                        if (c >= 128 || ValidUrlChars[c] == 0)
                        {
                            quickValid = false;
                            break;
                        }
                    }

                    // Full validation and processing of valid URLs
                    if (quickValid && IsValidUrl(urlStartPtr, urlLength))
                    {
                        int writePos = urlCount * MaxUrlLength;
                        int decodedLength = ProcessUrl(
                            urlStartPtr,
                            urlLength,
                            urlBufPtr + writePos);

                        if (decodedLength > 0)
                        {
                            urlLengths[urlCount++] = decodedLength;
                            if (urlCount >= urlLengths.Length)
                            {
                                return;
                            }
                        }
                    }
                }

                position += urlStart + urlLength;

                // Adjust chunk boundary if we've crossed it
                if (position > (int)(chunkEnd - bufPtr))
                {
                    chunkEnd = bufPtr + Math.Min(position + CHUNK_SIZE, safeLength);
                }
            }

            // Keep minimal bytes for next buffer
            remainingBytes = isLastBuffer ? 0 : Math.Min(MaxUrlLength, length - position);
        }
    }

    // Validates URL characters using SIMD when possible
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe bool IsValidUrl(byte* url, int length)
    {
        // Use SIMD for longer URLs
        if (Avx2.IsSupported && length >= 32)
        {
            fixed (byte* validPtr = ValidUrlChars)
            {
                int i = 0;
                // Process 32 bytes at a time
                while (i <= length - 32)
                {
                    var chunk = Avx2.LoadVector256(url + i);
                    // Check for high ASCII characters
                    if (Avx2.MoveMask(chunk) != 0) return false;

                    // Validate each character against lookup table
                    for (int j = 0; j < 32; j++)
                    {
                        if (validPtr[url[i + j]] == 0) return false;
                    }

                    i += 32;
                }

                // Handle remaining bytes
                for (; i < length; i++)
                {
                    byte c = url[i];
                    if (c >= 128 || ValidUrlChars[c] == 0) return false;
                }
            }

            return true;
        }

        // Scalar fallback for shorter URLs
        fixed (byte* validPtr = ValidUrlChars)
        {
            for (int i = 0; i < length; i++)
            {
                byte c = url[i];
                if (c >= 128 || validPtr[c] == 0) return false;
            }
        }

        return true;
    }

    // Processes URL encoding (handles %xx sequences)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int ProcessUrl(byte* source, int length, byte* destination)
    {
        int writePos = 0;
        fixed (int* hexPtr = HexLookup)
        {
            for (int i = 0; i < length && writePos < MaxUrlLength - 1; i++)
            {
                byte c = source[i];

                // Handle URL encoded characters (%xx)
                if (c == '%' && i + 2 < length)
                {
                    byte h1 = source[i + 1];
                    byte h2 = source[i + 2];

                    if (h1 < 128 && h2 < 128)
                    {
                        int hex1 = hexPtr[h1];
                        int hex2 = hexPtr[h2];

                        if (hex1 >= 0 && hex2 >= 0)
                        {
                            destination[writePos++] = (byte)((hex1 << 4) | hex2);
                            i += 2;
                            continue;
                        }
                    }
                }

                destination[writePos++] = c;
            }
        }

        return writePos;
    }

    // Finds 'href' pattern using SIMD acceleration
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int FindNextHrefOptimized(byte* buffer, int length)
    {
        if (length < 4) return -1;
        
        // Use SIMD for longer sequences
        if (length >= 32)
        {
            int i = 0;
            while (i <= length - 32)
            {
                // Load 32 bytes and make case-insensitive comparison for 'h'
                var data = Avx2.LoadVector256(buffer + i);
                var lower = Avx2.Or(data, LowerMask);
                var matches = Avx2.CompareEqual(lower, HChar);
                int mask = Avx2.MoveMask(matches);

                // Check each potential 'h' position
                while (mask != 0)
                {
                    int pos = i + BitOperations.TrailingZeroCount(mask);
                    if (pos <= length - 4)
                    {
                        // Verify full 'href' pattern
                        uint pattern = *(uint*)(buffer + pos) | 0x20202020;
                        if (pattern == HrefPattern)
                        {
                            return pos;
                        }
                    }

                    // Clear lowest set bit
                    mask &= mask - 1;
                }

                i += 32;
            }
        }

        // Scalar fallback for remaining bytes
        for (int i = 0; i <= length - 4; i++)
        {
            uint word = *(uint*)(buffer + i) | 0x20202020;
            if (word == HrefPattern)
            {
                return i;
            }
        }

        return -1;
    }
}