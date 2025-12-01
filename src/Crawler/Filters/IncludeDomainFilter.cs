using Crawler.Core;
using Crawler.Models;

namespace Crawler.Filters;

public class IncludeDomainFilter : ICrawlFilter
{
    private readonly ISet<string> _include;

    public IncludeDomainFilter(params string[] domains)
    {
        _include = new HashSet<string>(domains);
    }
    
    public bool ShouldCrawl(CrawlContext context)
    {
        return _include.Contains(context.Uri.Host);
    }
}