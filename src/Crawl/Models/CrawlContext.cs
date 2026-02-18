namespace Crawl.Models;

public readonly struct CrawlContext
{
    public CrawlContext(Uri uri, int depth = 0)
    {
        Uri = uri;
        Depth = depth;
    }

    public Uri Uri { get; }
    public int Depth { get; }
}