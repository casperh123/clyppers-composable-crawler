using Crawl.Core;
using Crawl.Core.Interfaces;
using Crawl.Filters;
using Crawl.Models;

namespace Crawler.Core.Tests.FilterTests;

public class IncludeDomainsFilterTests
{
    [Theory]
    [InlineData("http://somethingelse.com", false)]
    [InlineData("http://include.com", true)]
    public void ShouldCrawl(string uri, bool shouldCrawl)
    {
        ICrawlFilter filter = new IncludeDomainFilter("include.com");
        CrawlContext context = new CrawlContext { Uri = new Uri(uri) };

        bool computedShouldCrawl = filter.ShouldCrawl(context);
        
        Assert.Equal(computedShouldCrawl, shouldCrawl);
    }
}