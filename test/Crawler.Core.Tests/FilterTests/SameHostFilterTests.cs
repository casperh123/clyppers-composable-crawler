using Crawl.Core;
using Crawl.Core.Interfaces;
using Crawl.Filters;
using Crawl.Models;

namespace Crawler.Core.Tests.FilterTests;

public class SameHostFilterTests
{
     private readonly ICrawlFilter _sut = new SameHostFilter("https://somesite.com");

     [Fact]
     public void ShouldCrawl_ReturnTrue_OnSeedUrl()
     {
          Uri uri = new Uri("https://somesite.com");
          CrawlContext crawlContext = new CrawlContext(uri);

          bool shouldCrawl = _sut.ShouldCrawl(crawlContext);
          
          Assert.True(shouldCrawl);
     }
     
     [Fact]
     public void ShouldCrawl_ReturnsTrue_WhenSameSiteReferringUriAndUri()
     {
          Uri uri = new Uri("https://somesite.com/somesubpage");
          Uri referringUri = new Uri("https://somesite.com/");
          CrawlContext context = new CrawlContext(uri, referringUri);

          bool shouldCrawl = _sut.ShouldCrawl(context);
          
          Assert.True(shouldCrawl);
     }
     
     [Fact]
     public void ShouldCrawl_ReturnsFalse_WhenNotSameSiteReferringUri()
     {
          Uri uri = new Uri("https://othersite.com");
          Uri referringUri = new Uri("https://somesite.com/somesubpage");
          CrawlContext context = new CrawlContext(uri, referringUri);

          bool shouldCrawl = _sut.ShouldCrawl(context);
          
          Assert.False(shouldCrawl);
     }
     
     [Fact]
     public void ShouldCrawl_ReturnsFalse_WhenNotSameSiteUri()
     {
          Uri uri = new Uri("https://othersite.com/somesubpage");
          Uri referringUri = new Uri("https://somesite.com/");
          CrawlContext context = new CrawlContext(uri, referringUri);

          bool shouldCrawl = _sut.ShouldCrawl(context);
          
          Assert.False(shouldCrawl);
     }
}