using Crawler.Core;
using Crawler.Models;
using Crawler.Utils;

namespace Crawler.Filters;

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