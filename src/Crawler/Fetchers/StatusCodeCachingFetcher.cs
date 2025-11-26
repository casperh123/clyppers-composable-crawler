using System.Net;
using System.Runtime.InteropServices.Marshalling;
using Crawler.Core;
using Crawler.Models;

namespace Crawler.Fetchers;

public class StatusCodeCachingFetcher : IFetcher
{
    private readonly Dictionary<Uri, HttpStatusCode> _cache = [];
    private readonly IFetcher _fetcher;

    public StatusCodeCachingFetcher(IFetcher fetcher)
    {
        _fetcher = fetcher;
    }

    public StatusCodeCachingFetcher(HttpClient client)
    {
        _fetcher = new HttpFetcher(client);
    }


    public async Task<FetchResult> FetchAsync(CrawlContext context, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(context.Uri, out HttpStatusCode statusCode))
        {
            return new FetchResult
            {
                StatusCode = statusCode,
                Success = (int)statusCode >= 200 && (int)statusCode < 300
            };
        }

        FetchResult fetchResult = await _fetcher.FetchAsync(context, cancellationToken);

        _cache[context.Uri] = fetchResult.StatusCode;

        return fetchResult;
    }
}