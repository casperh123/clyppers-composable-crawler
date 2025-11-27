using Crawler.Core;
using Crawler.Models;

namespace Crawler.Filters;

public class SameHostFilter() : ICrawlFilter
{
    public bool ShouldCrawl(CrawlContext context)
    {
        if (context.ReferringUri is null)
            return true; 

        
        return context.Uri.Host.Equals(context.ReferringUri?.Host);
    }
}