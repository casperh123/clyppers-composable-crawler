using Crawl.Core;
using Crawl.Core.Interfaces;
using Crawl.Models;
using Crawl.Utils;

namespace Crawl.Filters;

public class SameHostFilter : ICrawlFilter
{
    private string? _rootHost;

    public SameHostFilter(string? uri = null)
    {
        if (uri is not null)
        {
            _rootHost = UriNormalizer.Normalize(new Uri(uri)).Host;   
        }
    } 
        

    public bool ShouldCrawl(CrawlContext context)
    {
        string normalizedHost = UriNormalizer.Normalize(context.Uri).Host;

        if (_rootHost is not null)
        {
            return normalizedHost.Equals(_rootHost);
        }
        
        _rootHost = normalizedHost;
        
        return true;
    }
}