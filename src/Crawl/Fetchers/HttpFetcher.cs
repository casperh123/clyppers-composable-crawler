using System.Diagnostics;
using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.Fetchers;

public class HttpFetcher : IFetcher
{
    private readonly HttpClient _client;
    private const int MaxContentLength = 10 * 1024 * 1024;
    
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/html",
        "application/xhtml+xml",
        "application/xml"
    };
    
    public HttpFetcher(HttpClient client)
    {
        _client = client;
    }
        
    public async ValueTask<FetchResult> FetchAsync(CrawlContext context, CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        
        using HttpResponseMessage response = await _client.GetAsync(
            context.Uri, 
            HttpCompletionOption.ResponseHeadersRead, 
            cancellationToken
        );

        TimeSpan ttfb = stopwatch.Elapsed;
        
        long? contentLength = response.Content.Headers.ContentLength;
        string? contentType = response.Content.Headers.ContentType?.MediaType;
        bool isHtml = contentType != null && AllowedContentTypes.Contains(contentType);

        string? content = null;

        if (response.IsSuccessStatusCode && isHtml && contentLength <= MaxContentLength)
        {
            content = await response.Content.ReadAsStringAsync(cancellationToken);
        }

        TimeSpan requestDuration = stopwatch.Elapsed;
        
        return new FetchResult
        {
            Uri = context.Uri,
            Content = content,
            Success = response.IsSuccessStatusCode,
            StatusCode = response.StatusCode,
            ContentType = response.Content.Headers.ContentType?.MediaType,
            Headers = response.Headers,
            TTFB = ttfb,
            RequestDuration = requestDuration
        };
    }
}