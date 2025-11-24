using Crawler.Core;
using Crawler.Models;

namespace Crawler.Filters;

public class SameHostFilter() : ICrawlFilter
{
    public bool ShouldCrawl(CrawlContext context)
    {
        Uri? referringUri = context.ReferringUri;
        Uri uri = context.Uri;

        return uri.Host.Equals(referringUri?.Host);
    }
}