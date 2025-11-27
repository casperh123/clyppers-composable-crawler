using System.Net;
using Crawler.Fetchers;
using Crawler.Models;

namespace Crawler.Core.Tests.FetcherTests;

public class StatusCodeCachingFetcherTests
{
    private readonly StatusCodeCachingFetcher _sut;

    public StatusCodeCachingFetcherTests()
    {
        HttpClient client = new HttpClient();
        _sut = new StatusCodeCachingFetcher(client);
    }

    [Fact]
    public async Task FetchAsync_Returns200()
    {
        Uri uri = new Uri("https://crawler-test.com/status_codes/status_200");
        CrawlContext context = new CrawlContext { Uri = uri };

        FetchResult result = await _sut.FetchAsync(context);
        
        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task FetchAsync_WhenStatusCodeSeen_ReturnsCachedStatusCode()
    {
        Uri uri = new Uri("https://crawler-test.com/status_codes/status_200");
        CrawlContext context = new CrawlContext { Uri = uri };

        FetchResult uncachedResult = await _sut.FetchAsync(context);
        FetchResult cachedResult = await _sut.FetchAsync(context);

        string? uncachedResultContent = uncachedResult.Content;
        string? cachedResultContent = cachedResult.Content;

        Assert.True(uncachedResult.Success);
        Assert.True(cachedResult.Success);
        Assert.NotNull(uncachedResultContent);
        Assert.NotEmpty(uncachedResultContent);
        Assert.Null(cachedResultContent);
    }
    
}