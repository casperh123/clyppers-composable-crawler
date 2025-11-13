using System.Runtime.CompilerServices;
using System.Text;

namespace BrokenLinkChecker.DocumentParsing.ModularLinkExtraction.FastParse;

public unsafe class UrlCreater
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ConvertUrlsToStrings(
        byte[] urlBuffer,
        int[] urlLengths,
        int count,
        int maxUrlLength,
        List<string> output)
    {
        fixed (byte* urlBufferPtr = urlBuffer)
        {
            for (int i = 0; i < count; i++)
            {
                int byteLength = urlLengths[i];
                if (byteLength > 0 && byteLength < maxUrlLength)
                {
                    ReadOnlySpan<byte> urlBytes = new ReadOnlySpan<byte>(urlBufferPtr + i * maxUrlLength, byteLength);
                    int charCount = Encoding.UTF8.GetCharCount(urlBytes);
                    string url = string.Create(charCount, urlBytes.ToArray(), (chars, state) =>
                    {
                        fixed (byte* bytes = state)
                        {
                            fixed (char* dest = chars)
                            {
                                Encoding.UTF8.GetChars(bytes, state.Length, dest, chars.Length);
                            }
                        }
                    });
                    output.Add(url);
                }
            }
        }
    }
}