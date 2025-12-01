namespace Crawl.Models;

public record CrawlResult
{
    public required CrawlContext Context { get; init; }
    public required FetchResult FetchResult { get; init; }
    public required ICollection<DiscoveredLink> DiscoveredLinks { get; init; }
    public required TimeSpan ElapsedTime { get; init; }
}