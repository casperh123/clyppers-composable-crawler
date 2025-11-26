using Crawler.Filters;
using Crawler.Models;

namespace Crawler.Core.Tests.FilterTests;

public class SameHostFilterTests
{
     private readonly ICrawlFilter _sut = new SameHostFilter();

     [Fact]
     public void ShouldCrawl_ReturnTrue_OnSeedUrl()
     {
          Uri uri = new Uri("https://somesite.com");
          CrawlContext crawlContext = new CrawlContext {  Uri = uri };

          bool shouldCrawl = _sut.ShouldCrawl(crawlContext);
          
          Assert.True(shouldCrawl);
     }
     
     [Fact]
     public void ShouldCrawl_ReturnsTrue_WhenSameSiteReferringUriAndUri()
     {
          Uri uri = new Uri("https://somesite.com/somesubpage");
          Uri referringUri = new Uri("https://somesite.com/");
          CrawlContext context = new CrawlContext { Uri = uri, ReferringUri = referringUri};

          bool shouldCrawl = _sut.ShouldCrawl(context);
          
          Assert.True(shouldCrawl);
     }
     
     [Fact]
     public void ShouldCrawl_ReturnsFalse_WhenNotSameSiteReferringUri()
     {
          Uri uri = new Uri("https://somesite.com/somesubpage");
          Uri referringUri = new Uri("https://othersite.com/");
          CrawlContext context = new CrawlContext { Uri = uri, ReferringUri = referringUri};

          bool shouldCrawl = _sut.ShouldCrawl(context);
          
          Assert.False(shouldCrawl);
     }
     
     [Fact]
     public void ShouldCrawl_ReturnsFalse_WhenNotSameSiteUri()
     {
          Uri uri = new Uri("https://othersite.com/somesubpage");
          Uri referringUri = new Uri("https://somesite.com/");
          CrawlContext context = new CrawlContext { Uri = uri, ReferringUri = referringUri};

          bool shouldCrawl = _sut.ShouldCrawl(context);
          
          Assert.False(shouldCrawl);
     }
}