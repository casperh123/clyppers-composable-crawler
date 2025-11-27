using Crawler.Core;
using Crawler.Models;
using Crawler.Utils;

namespace Crawler.Filters;

public class SameHostFilter : ICrawlFilter
{
    private string? _rootHost;

    public bool ShouldCrawl(CrawlContext context)
    {
        var normalizedHost = UriNormalizer.Normalize(context.Uri).Host;

        if (_rootHost is null)
        {
            _rootHost = normalizedHost;
            return true;
        }

        return normalizedHost.Equals(_rootHost);
    }
}