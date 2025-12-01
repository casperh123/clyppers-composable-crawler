using Crawl.Core;
using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.Filters;

public class InertFilter : ICrawlFilter
{
    public bool ShouldCrawl(CrawlContext context)
    {
        return true;
    }
}