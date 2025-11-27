using Crawler.Core;
using Crawler.Models;

namespace Crawler.Fetchers;

public class HttpFetcher : IFetcher
{
    private readonly HttpClient _client;
    
    public HttpFetcher(HttpClient client)
    {
        _client = client;
    }
        
    public async Task<FetchResult> FetchAsync(CrawlContext context, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _client.GetAsync(
            context.Uri, 
            HttpCompletionOption.ResponseHeadersRead, 
            cancellationToken
        );

        string? content = response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync(cancellationToken)
            : null;

        return new FetchResult
        {
            Uri = context.Uri,
            Content = content,
            Success = response.IsSuccessStatusCode,
            StatusCode = response.StatusCode,
            ContentType = response.Content.Headers.ContentType?.MediaType
        };
    }
}