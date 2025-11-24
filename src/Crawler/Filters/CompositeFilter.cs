using Crawler.Core;
using Crawler.Models;

namespace Crawler.Filters;

public class CompositeFilter : ICrawlFilter
{
    private IEnumerable<ICrawlFilter> _filters;

    public CompositeFilter(params ICrawlFilter[] filters)
    {
        _filters = filters;
    }


    public bool ShouldCrawl(CrawlContext context)
    {
        return _filters.All(filter => filter.ShouldCrawl(context));
    }
}