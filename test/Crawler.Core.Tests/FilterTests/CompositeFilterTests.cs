using Crawl.Filters;
using Crawl.Models;

namespace Crawler.Core.Tests.FilterTests;

public class CompositeFilterTests
{
    private readonly ICrawlFilter _sut = new CompositeFilter(new SameHostFilter("https://somesite.com"));

    [Fact]
    public void ShouldCrawl_ReturnsTrue_WhenSameSiteAndNotDuplicate()
    {
        Uri uri = new Uri("https://somesite.com");
        Uri referringUri = new Uri("https://somesite.com/somepage");
        CrawlContext context = new CrawlContext { Uri = uri, ReferringUri = referringUri };

        bool shouldCrawl = _sut.ShouldCrawl(context);
        
        Assert.True(shouldCrawl);
    }
}