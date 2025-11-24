using Crawler.Filters;
using Crawler.Models;

namespace Crawler.Core.Tests.FilterTests;

public class CompositeFilterTests
{
    private readonly ICrawlFilter _sut = new CompositeFilter(new SameHostFilter(), new DeduplicationFilter());

    [Fact]
    public void ShouldCrawl_ReturnsTrue_WhenSameSiteAndNotDuplicate()
    {
        Uri uri = new Uri("https://somesite.com");
        Uri referringUri = new Uri("https://somesite.com/somepage");
        CrawlContext context = new CrawlContext { Uri = uri, ReferringUri = referringUri };

        bool shouldCrawl = _sut.ShouldCrawl(context);
        
        Assert.True(shouldCrawl);
    }
    
    [Fact]
    public void ShouldCrawl_ReturnsFalse_WhenNotSameSiteAndNotDuplicate()
    {
        Uri uri = new Uri("https://somesite.com");
        Uri referringUri = new Uri("https://othersite.com");
        CrawlContext context = new CrawlContext { Uri = uri, ReferringUri = referringUri };

        bool shouldCrawl = _sut.ShouldCrawl(context);
        
        Assert.False(shouldCrawl);
    }
    
    [Fact]
    public void ShouldCrawl_ReturnsFalse_WhenSameSiteAndDuplicate()
    {
        Uri uri = new Uri("https://somesite.com/page");
        Uri referringUri = new Uri("https://somesite.com/page");
        CrawlContext context = new CrawlContext { Uri = uri, ReferringUri = referringUri };

        _sut.ShouldCrawl(context);
        bool crawledDuplicate = _sut.ShouldCrawl(context);
        
        Assert.False(crawledDuplicate);
    }
}