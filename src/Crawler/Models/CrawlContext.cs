namespace Crawler.Models;

public record CrawlContext
{
    public required  Uri Uri { get; set; }
    public Uri? ReferringUri { get; set; }
    public int Depth { get; set; }

    public static CrawlContext From(Uri uri, Uri? referringUri, int depth)
    {
        return new CrawlContext
        {
            Uri = uri,
            ReferringUri = referringUri,
            Depth = depth
        };
    }
}