using Crawl.Core;
using Crawl.Models;

namespace Crawl.Filters;

public class IncludeDomainFilter : ICrawlFilter
{
    private readonly ISet<string> _include;

    public IncludeDomainFilter(params string[] domains)
    {
        _include = new HashSet<string>(domains);
    }
    
    public IncludeDomainFilter(ISet<string> domains)
    {
        _include = domains;
    }
    
    public bool ShouldCrawl(CrawlContext context)
    {
        return _include.Contains(context.Uri.Host);
    }
}