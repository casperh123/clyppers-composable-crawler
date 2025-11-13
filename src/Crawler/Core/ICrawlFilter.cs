using Crawler.Models;

namespace Crawler.Core;

public interface ICrawlFilter
{
    bool ShouldCrawl(CrawlContext context);
}