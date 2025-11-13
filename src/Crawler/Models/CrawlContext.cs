namespace Crawler.Models;

public record CrawlContext
{
    public required  Uri Uri { get; set; }
    public Uri? ReferringUri { get; set; }
    public int Depth { get; set; }

    public static CrawlContext FromDiscoveredLink(DiscoveredLink discoveredLink, int depth)
    {
        return new CrawlContext
        {
            Uri = discoveredLink.Uri,
            ReferringUri = discoveredLink.ReferringUri,
            Depth = depth
        };
    }
}