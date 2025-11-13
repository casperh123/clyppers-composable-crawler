using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace BrokenLinkChecker.DocumentParsing.ModularLinkExtraction.FastParse;

/// <summary>
/// High-performance quote finder that uses SIMD (Single Instruction Multiple Data) operations
/// to process multiple bytes in parallel. Falls back to scalar operations for edge cases and
/// when SIMD isn't supported.
/// </summary>
public class QuoteFinder
{
    // Pre-computed SIMD vectors for pattern matching
    // These vectors fill all 32 bytes with the same character for efficient comparison
    private static readonly Vector256<byte> SingleQuotes = Vector256.Create((byte)'\'');
    private static readonly Vector256<byte> DoubleQuotes = Vector256.Create((byte)'\"');

    /// <summary>
    /// Finds the first valid quote pair in a byte buffer, using SIMD optimizations where possible.
    /// Returns the position and length of the content between the quotes.
    /// </summary>
    /// <param name="buffer">Pointer to the start of the byte buffer to search</param>
    /// <param name="length">Total length of the buffer in bytes</param>
    /// <returns>A QuotePosition containing the start position and length of found quote content, or default if none found</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]  // Hint to compiler to inline this performance-critical method
    public static unsafe QuotePosition FindQuoteOptimized(byte* buffer, int length)
    {
        // Fall back to scalar processing for small buffers or when AVX2 SIMD isn't supported
        if (length < 32 || !Avx2.IsSupported)
        {
            return FindQuoteScalar(buffer, 0, length);
        }

        int position = 0;

        // Process the buffer in 32-byte chunks with 8-byte overlap
        // The overlap ensures we don't miss quotes that span SIMD block boundaries
        while (position <= length - 32)
        {
            // Load 32 bytes into a SIMD register
            var data = Avx2.LoadVector256(buffer + position);
            
            // Check for both single and double quotes in parallel
            var singleMatches = Avx2.CompareEqual(data, SingleQuotes);
            var doubleMatches = Avx2.CompareEqual(data, DoubleQuotes);
            
            // Combine the match results into a single mask
            var combinedMatches = Avx2.Or(singleMatches, doubleMatches);
            int mask = Avx2.MoveMask(combinedMatches);  // Convert matches to a 32-bit mask

            // Process each quote found in the current SIMD block
            while (mask != 0)
            {
                // Find position of the next quote using bit manipulation
                int quoteOffset = BitOperations.TrailingZeroCount(mask);
                int quotePos = position + quoteOffset;

                // Try to find a matching closing quote
                var result = FindMatchingQuote(buffer, quotePos, length);
                if (result.IsValid)
                {
                    return result;
                }

                // Clear the processed quote bit and continue with next quote
                mask &= ~(1 << quoteOffset);
            }

            // Advance position with overlap
            position += 24;  // 32 - 8 bytes overlap
        }

        // Handle any remaining bytes that don't fill a complete SIMD block
        if (position < length)
        {
            return FindQuoteScalar(buffer, position, length - position);
        }

        return default;
    }

    /// <summary>
    /// Scalar (non-SIMD) implementation for finding quotes. Used for small buffers
    /// or when SIMD processing isn't available/practical.
    /// </summary>
    /// <param name="buffer">Buffer to search</param>
    /// <param name="startPos">Starting position in buffer</param>
    /// <param name="remainingLength">Number of bytes to process</param>
    /// <returns>QuotePosition for first valid quote pair found, or default if none found</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe QuotePosition FindQuoteScalar(byte* buffer, int startPos, int remainingLength)
    {
        byte* ptr = buffer + startPos;
        byte* end = buffer + startPos + remainingLength;

        // Simple byte-by-byte scan for quotes
        while (ptr < end)
        {
            if (*ptr == '\'' || *ptr == '\"')
            {
                int absolutePos = (int)(ptr - buffer);
                var result = FindMatchingQuote(buffer, absolutePos, startPos + remainingLength);
                if (result.IsValid)
                {
                    return result;
                }
            }
            ptr++;
        }

        return default;
    }

    /// <summary>
    /// Helper method that finds a matching closing quote given an opening quote position.
    /// Handles escaped quotes (quotes preceded by backslash) within the content.
    /// </summary>
    /// <param name="buffer">Buffer containing the quotes</param>
    /// <param name="openQuotePos">Position of the opening quote</param>
    /// <param name="length">Total length of buffer</param>
    /// <returns>QuotePosition with the quote content details if found, or default if no matching quote</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe QuotePosition FindMatchingQuote(byte* buffer, int openQuotePos, int length)
    {
        // Get the quote character we're looking for (single or double)
        byte quote = buffer[openQuotePos];
        byte* searchStart = buffer + openQuotePos + 1;
        byte* endPtr = buffer + length;

        // Scan for matching quote, handling escaped quotes
        for (byte* ptr = searchStart; ptr < endPtr; ptr++)
        {
            // Skip escaped quotes (e.g., \' or \")
            if (*ptr == '\\' && ptr + 1 < endPtr && ptr[1] == quote)
            {
                ptr++;  // Skip the escaped quote character
                continue;
            }
            
            // Found matching quote - calculate content length excluding quotes
            if (*ptr == quote)
            {
                int contentLength = (int)(ptr - (buffer + openQuotePos + 1));
                return new QuotePosition(openQuotePos, contentLength, true);
            }
        }

        return default;  // No matching quote found
    }
}