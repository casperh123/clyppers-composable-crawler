namespace Crawl.Models;

public readonly struct CrawlContext
{
    public CrawlContext(Uri uri, Uri? referringUri = null, int depth = 0)
    {
        Uri = uri;
        ReferringUri = referringUri;
        Depth = depth;
    }

    public Uri Uri { get; }
    public Uri? ReferringUri { get; }
    public int Depth { get; }

    public static CrawlContext From(Uri uri, Uri? referringUri, int depth)
        => new(uri, referringUri, depth);
}