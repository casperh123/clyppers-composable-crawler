using Crawler.Core;
using Crawler.Models;

namespace Crawler.Filters;

public class ExcludeDomainsFilter : ICrawlFilter
{
    private readonly ISet<string> _exclude;

    public ExcludeDomainsFilter(params string[] domains)
    {
        _exclude = new HashSet<string>(domains);
    }
    
    public bool ShouldCrawl(CrawlContext context)
    {
        return !_exclude.Contains(context.Uri.Host);
    }
}