using Crawler.Filters;
using Crawler.Models;

namespace Crawler.Core.Tests.FilterTests;

public class DeduplicationFilterTests
{
    private readonly ICrawlFilter _sut = new DeduplicationFilter();

    [Fact]
    public void ShouldCrawl_ReturnsTrue_WhenUriNotCrawled()
    {
        Uri uri = new Uri("https://samesite.com/page");
        Uri referringUri = new Uri("https://samesite.com/otherpage");
        CrawlContext context = new CrawlContext { Uri = uri, ReferringUri = referringUri };

        bool shouldCrawl = _sut.ShouldCrawl(context);
        
        Assert.True(shouldCrawl);
    }
    
    [Fact]
    public void ShouldCrawl_ReturnsFalse_WhenUriAlreadyCrawled()
    {
        Uri uri = new Uri("https://samesite.com/page");
        Uri referringUri = new Uri("https://samesite.com/page");
        CrawlContext context = new CrawlContext { Uri = uri, ReferringUri = referringUri };

        _sut.ShouldCrawl(context);
        bool crawledDuplicate = _sut.ShouldCrawl(context);
        
        Assert.False(crawledDuplicate);
    }
}