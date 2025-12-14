namespace Crawl.Models;

public record CrawlTiming
{
    public required Uri Uri { get; init; }
    public required TimeSpan? TTFB { get; init; }
    public required TimeSpan? RequestDuration { get; set; }
    public required TimeSpan? ElapsedTime { get; set; }
}