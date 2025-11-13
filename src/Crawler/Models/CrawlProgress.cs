namespace Crawler.Models;

public record CrawlProgress
{
    public CrawlContext? Context { get; init; }
    public int TotalCrawled { get; init; }
    public int QueueSize { get; init; }
    public string? ErrorMessage { get; init; }

    public static CrawlProgress Started()
    {
        return new CrawlProgress {
            TotalCrawled = 0,
            QueueSize = 0
        };
    }

    public static CrawlProgress Error(
        CrawlContext context,
        Exception Error,
        int totalCrawled,
        int queueSize
        ) {
        return new CrawlProgress
        {
            Context = context,
            ErrorMessage = Error.Message,
            TotalCrawled = totalCrawled,
            QueueSize = queueSize
        };
    }
}