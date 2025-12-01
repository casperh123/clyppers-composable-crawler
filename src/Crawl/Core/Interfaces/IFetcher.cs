using Crawl.Models;

namespace Crawl.Core;

public interface IFetcher
{
    Task<FetchResult> FetchAsync(CrawlContext context, CancellationToken cancellationToken = default);
}