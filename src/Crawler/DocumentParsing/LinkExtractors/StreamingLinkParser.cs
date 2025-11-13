using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

namespace BrokenLinkChecker.DocumentParsing.ModularLinkExtraction;

public class StreamingLinkParser
{
    // Use a HashSet for automatic uniqueness checks, avoiding Distinct().
    private static readonly Regex LinkRegex = new Regex(
        @"<a\s+[^>]*\s*href\s*=\s*(?:['""""])(?<href>https?://[^'""\s]+)(?:['""""])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);


    public async Task<IEnumerable<string>> ParseLinksAsync(Stream htmlStream)
    {
        var links = new HashSet<string>();
        char[] buffer = new char[32768]; // Buffer size for streaming
        int leftoverLength = 0;

        using var reader = new StreamReader(htmlStream, Encoding.UTF8);

        int bytesRead;
        while ((bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            // Process the new chunk and leftover from the previous chunk
            ReadOnlySpan<char> chunk = new ReadOnlySpan<char>(buffer, 0, buffer.Length);

            Regex.ValueMatchEnumerator match = LinkRegex.EnumerateMatches(chunk);

            while (match.MoveNext())
            {
                ValueMatch currentMatch = match.Current;

                // Get the matched span for the entire <a> tag
                ReadOnlySpan<char> matchChunk = chunk.Slice(currentMatch.Index, currentMatch.Length);

                // Locate the start of the href value (after 'href="')
                int hrefStartIndex = matchChunk.IndexOf("href=\"") + 6; // 6 is the length of 'href="'
                if (hrefStartIndex >= 0)
                {
                    // Locate the end of the href value (before closing '"')
                    int hrefEndIndex = matchChunk.Slice(hrefStartIndex).IndexOf('"');

                    if (hrefEndIndex >= 0)
                    {
                        // Extract the link (the substring between the quotes)
                        ReadOnlySpan<char> linkSpan = matchChunk.Slice(hrefStartIndex, hrefEndIndex);

                        // Convert the span to string and add to the set
                        links.Add(linkSpan.ToString());
                    }
                }
            }
        }

        return links;
    }
}