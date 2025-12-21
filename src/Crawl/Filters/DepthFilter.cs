using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.Filters;

public class DepthFilter(int depth) : ICrawlFilter
{
    
    
    public bool ShouldCrawl(CrawlContext context)
    {
        if (context.Depth > depth)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}