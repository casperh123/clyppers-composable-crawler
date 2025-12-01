using Crawl.Core;
using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.Filters;

public class ExcludeDomainsFilter : ICrawlFilter
{
    private readonly ISet<string> _exclude;

    public ExcludeDomainsFilter(params string[] domains)
    {
        _exclude = new HashSet<string>(domains);
    }
    
    public ExcludeDomainsFilter(ISet<string> domains)
    {
        _exclude = domains;
    }
    
    public bool ShouldCrawl(CrawlContext context)
    {
        return !_exclude.Contains(context.Uri.Host);
    }
}