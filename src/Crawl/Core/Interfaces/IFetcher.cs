using Crawl.Models;

namespace Crawl.Core.Interfaces;

public interface IFetcher
{
    ValueTask<FetchResult> FetchAsync(CrawlContext context, CancellationToken cancellationToken = default);
}