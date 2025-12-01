using Crawl.Core;
using Crawl.Models;

namespace Crawl.Filters;

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