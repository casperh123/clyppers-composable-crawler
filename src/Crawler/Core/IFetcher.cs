using Crawler.Models;

public interface IFetcher
{
    Task<FetchResult> FetchAsync(CrawlContext context, CancellationToken cancellationToken = default);
}