using Crawl.Core;
using Crawl.Models;

namespace Crawl.Filters;

public class InertFilter : ICrawlFilter
{
    public bool ShouldCrawl(CrawlContext context)
    {
        return true;
    }
}