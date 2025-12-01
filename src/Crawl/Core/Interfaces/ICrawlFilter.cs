using Crawl.Models;

namespace Crawl.Core.Interfaces;

public interface ICrawlFilter
{
    bool ShouldCrawl(CrawlContext context);
}