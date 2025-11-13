using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace BrokenLinkChecker.DocumentParsing.ModularLinkExtraction;

public static class ParallelLinkExtractor
{
    private const int BufferSize = 32768; // 32KB chunks
    private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;
    private static readonly Vector256<byte> HrefLowerMask = Vector256.Create((byte)'h');

    public static async Task<List<string>> ExtractHrefsParallelAsync(Stream responseStream)
    {
        ConcurrentBag<string> links = new ConcurrentBag<string>();

        // Read stream into memory in chunks, then process
        using var ms = new MemoryStream();
        byte[] buffer = BufferPool.Rent(BufferSize);

        try
        {
            int bytesRead;

            // Read stream in chunks
            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, BufferSize)) > 0)
            {
                await ms.WriteAsync(buffer, 0, bytesRead);
            }

            // Get the complete data
            byte[] fullData = ms.ToArray();

            // Create partitions for parallel processing
            var partitioner = Partitioner.Create(0, fullData.Length,
                Math.Min(BufferSize, fullData.Length / Environment.ProcessorCount));

            // Process partitions in parallel
            await Task.WhenAll(partitioner.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(range => Task.Run(() =>
                    ProcessPartition(fullData, range.Item1, range.Item2, links)))
                .ToArray());

            return links.ToList();
        }
        finally
        {
            BufferPool.Return(buffer);
        }
    }

    private static unsafe void ProcessPartition(byte[] buffer, int start, int end, ConcurrentBag<string> links)
    {
        fixed (byte* ptr = buffer)
        {
            ProcessChunk(ptr + start, end - start, links);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ProcessChunk(byte* buffer, int length, ConcurrentBag<string> links)
    {
        int position = 0;
        while (position < length - 4)
        {
            int hrefPos = FindNextHref(buffer + position, length - position);
            if (hrefPos == -1) break;

            position += hrefPos;

            while (position < length)
            {
                byte c = buffer[position++];
                if (c == '"' || c == '\'')
                {
                    byte quote = c;
                    int endPos = FindQuote(buffer + position, length - position, quote);
                    if (endPos == -1) break;

                    if (endPos > 0)
                    {
                        ProcessLink(buffer + position, endPos, links);
                    }

                    position += endPos + 1;
                    break;
                }

                if (c == '>' || position >= length) break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ProcessLink(byte* source, int length, ConcurrentBag<string> links)
    {
        if (length == 0 || length > 2048) return;

        const int maxStackSize = 256;
        if (length <= maxStackSize)
        {
            char* chars = stackalloc char[length];
            int charCount = 0;

            bool inContent = false;
            int lastNonSpace = -1;

            for (int i = 0; i < length; i++)
            {
                char c = (char)source[i];
                if (c > 32)
                {
                    if (!inContent)
                    {
                        inContent = true;
                        charCount = 0;
                    }

                    chars[charCount++] = c;
                    lastNonSpace = charCount - 1;
                }
                else if (inContent)
                {
                    chars[charCount++] = c;
                }
            }

            if (lastNonSpace >= 0)
            {
                links.Add(new string(chars, 0, lastNonSpace + 1));
            }
        }
        else
        {
            var str = Encoding.ASCII.GetString(source, length).Trim();
            if (str.Length > 0)
            {
                links.Add(str);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int FindQuote(byte* buffer, int length, byte quote)
    {
        if (Avx2.IsSupported && length >= 32)
        {
            var vQuote = Vector256.Create(quote);

            int i = 0;
            while (i <= length - 32)
            {
                var data = Avx2.LoadVector256(buffer + i);
                var matches = Avx2.CompareEqual(data, vQuote);
                int mask = Avx2.MoveMask(matches);

                if (mask != 0)
                    return i + BitOperations.TrailingZeroCount(mask);

                i += 32;
            }

            length = i;
        }

        for (int i = 0; i < length; i++)
            if (buffer[i] == quote)
                return i;

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int FindNextHref(byte* buffer, int length)
    {
        if (Avx2.IsSupported && length >= 32)
        {
            var caseMask = Vector256.Create((byte)0x20);

            int i = 0;
            while (i <= length - 32)
            {
                var data = Avx2.LoadVector256(buffer + i);
                var normalized = Avx2.Or(data, caseMask);
                var matches = Avx2.CompareEqual(normalized, HrefLowerMask);
                int mask = Avx2.MoveMask(matches);

                while (mask != 0)
                {
                    int pos = i + BitOperations.TrailingZeroCount(mask);
                    if (pos <= length - 4)
                    {
                        uint word = *(uint*)(buffer + pos) | 0x20202020;
                        if ((word & 0xFF) == 'h' &&
                            ((word >> 8) & 0xFF) == 'r' &&
                            ((word >> 16) & 0xFF) == 'e' &&
                            ((word >> 24) & 0xFF) == 'f')
                        {
                            return pos;
                        }
                    }

                    mask &= mask - 1;
                }

                i += 32 - 3;
            }

            length = i;
        }

        for (int i = 0; i <= length - 4; i++)
        {
            uint word = *(uint*)(buffer + i) | 0x20202020;
            if ((word & 0xFF) == 'h' &&
                ((word >> 8) & 0xFF) == 'r' &&
                ((word >> 16) & 0xFF) == 'e' &&
                ((word >> 24) & 0xFF) == 'f')
            {
                return i;
            }
        }

        return -1;
    }
}