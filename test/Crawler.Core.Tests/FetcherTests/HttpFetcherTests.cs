using System.Net;
using Crawl.Fetchers;
using Crawl.Models;

namespace Crawler.Core.Tests.FetcherTests;

public class HttpFetcherTests
{
    private readonly HttpFetcher _sut;

    public HttpFetcherTests()
    {
        HttpClient client = new HttpClient();
        _sut = new HttpFetcher(client);
    }

    [Fact]
    public async Task FetchAsync_Returns200()
    {
        Uri testUri = new Uri("https://crawler-test.com/status_codes/status_200");
        CrawlContext context = new CrawlContext { Uri = testUri };

        FetchResult result = await _sut.FetchAsync(context);
        
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
    
    [Fact]
    public async Task FetchAsync_PopulatesEntireFetchResult()
    {
        Uri testUri = new Uri("https://crawler-test.com/status_codes/status_200");
        CrawlContext context = new CrawlContext { Uri = testUri };

        FetchResult result = await _sut.FetchAsync(context);
        string? stringResult = result.Content;
        string? byteArrayResult = result.Content;
        
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.True(result.Success);
        Assert.NotNull(byteArrayResult);
        Assert.NotEmpty(byteArrayResult);
        Assert.NotNull(stringResult);
        Assert.NotEmpty(stringResult);
    }
    
    [Fact]
    public async Task FetchAsync_sucessIsFalseWhen_StatusCode500()
    {
        Uri testUri = new Uri("https://crawler-test.com/status_codes/status_500");
        CrawlContext context = new CrawlContext { Uri = testUri };

        FetchResult result = await _sut.FetchAsync(context);
        
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.False(result.Success);
    }
}