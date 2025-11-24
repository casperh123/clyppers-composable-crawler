using Crawler.Models;

namespace Crawler.Core;

public interface IFetcher
{
    Task<FetchResult> FetchAsync(CrawlContext context, CancellationToken cancellationToken = default);
}