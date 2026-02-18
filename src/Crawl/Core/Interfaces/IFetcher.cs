using Crawl.Models;

namespace Crawl.Core.Interfaces;

public interface IFetcher
{
    ValueTask<FetchResult> FetchAsync(Uri uri, CancellationToken cancellationToken = default);
}