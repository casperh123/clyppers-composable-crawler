using Crawler.Core;
using Crawler.Models;

namespace Crawler.Filters;

public class InertFilter : ICrawlFilter
{
    public bool ShouldCrawl(CrawlContext context)
    {
        return true;
    }
}