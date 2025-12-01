using Crawl.Models;

namespace Crawl.Core;

public interface ICrawlFilter
{
    bool ShouldCrawl(CrawlContext context);
}