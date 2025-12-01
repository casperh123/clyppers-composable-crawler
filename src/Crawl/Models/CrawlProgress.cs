namespace Crawl.Models;

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

    public static CrawlProgress Completed(int totalCrawled)
    {
        return new CrawlProgress
        {
            TotalCrawled = totalCrawled
        };
    }

    public static CrawlProgress Progress(
        CrawlContext context,
        int totalCrawled,
        int queueSize
    )
    {
        return new CrawlProgress
        {
            Context = context,
            TotalCrawled = totalCrawled,
            QueueSize = queueSize
        };
    }

    public static CrawlProgress Error(
        CrawlContext context,
        Exception error,
        int totalCrawled,
        int queueSize
        ) {
        return new CrawlProgress
        {
            Context = context,
            ErrorMessage = error.Message,
            TotalCrawled = totalCrawled,
            QueueSize = queueSize
        };
    }
}