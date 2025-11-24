using Crawler.Filters;

namespace Crawler.Core;

public class CrawlerBuilder
{
    private ICrawlFilter _filter;
    private IFetcher _fetcher;
    private ILinkDiscoverer _discoverer;
    private ICrawlVisitor _visitor;

    public CrawlerBuilder()
    {
        _filter = new InertFilter();
    }

    public CrawlerBuilder WithFilter(ICrawlFilter filter)
    {
        _filter = filter;
        
        return this;
    }

    public CrawlerBuilder WithFilters(params ICrawlFilter[] filters)
    {
        _filter = new CompositeFilter(filters);

        return this;
    }

    public Crawler Build()
    {
        return new Crawler(
            _filter,
            _fetcher,
            _discoverer,
            _visitor
        );
    }
}