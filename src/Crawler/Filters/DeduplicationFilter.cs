using Crawler.Core;
using Crawler.Models;

namespace Crawler.Filters;

public class DeduplicationFilter : ICrawlFilter
{
    private HashSet<Uri> _visited = new HashSet<Uri>();
    
    public bool ShouldCrawl(CrawlContext context)
    {
        return _visited.Add(context.Uri);
    }
}