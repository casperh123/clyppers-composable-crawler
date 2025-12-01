using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.Filters;

public class IncludeLTTsFilter : ICrawlFilter
{
    private readonly ISet<string> _include;
    
    public IncludeLTTsFilter(params string[] ltts)
    {
        _include = new HashSet<string>(ltts);
    }
    
    public bool ShouldCrawl(CrawlContext context)
    {
        string host = context.Uri.Host;
    
        int lastDot = host.LastIndexOf('.');
        if (lastDot < 0 || lastDot == host.Length - 1)
            return false;

        string tld = host.Substring(lastDot + 1);

        return _include.Contains(tld);
    }
}