namespace Crawler.Models;

public record CrawlResult
{
    public CrawlContext Context { get; set; }
    public FetchResult FetchResult { get; set; }
    public ICollection<DiscoveredLink> DiscoveredLinks { get; set; }
    public TimeSpan ElapsedTime { get; set; }
}