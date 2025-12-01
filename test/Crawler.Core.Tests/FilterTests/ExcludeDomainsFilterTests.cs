using Crawl.Core;
using Crawl.Core.Interfaces;
using Crawl.Filters;
using Crawl.Models;

namespace Crawler.Core.Tests.FilterTests;

public class ExcludeDomainsFilterTests
{
    public ExcludeDomainsFilterTests()
    {
        
    }

    [Theory]
    [InlineData("http://somethingelse.com", true)]
    [InlineData("http://toexclude.com", false)]
    public void ShouldCrawl(string uri, bool shouldCrawl)
    {
        ICrawlFilter filter = new ExcludeDomainsFilter("toexclude.com");
        CrawlContext context = new CrawlContext(new Uri(uri));

        bool computedShouldCrawl = filter.ShouldCrawl(context);
        
        Assert.Equal(computedShouldCrawl, shouldCrawl);
    }
}