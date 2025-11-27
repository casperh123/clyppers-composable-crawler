using Crawler.Fetchers;
using Crawler.Filters;
using Crawler.LinkDiscoverers;
using Crawler.Visitors;

namespace Crawler.Core;

public class CrawlerBuilder
{
    private readonly IList<ICrawlFilter> _filters = [];
    private IFetcher _fetcher;
    private ILinkDiscoverer _discoverer;
    private ICrawlVisitor _visitor;

    public CrawlerBuilder(HttpClient httpClient)
    {
        _fetcher = new HttpFetcher(httpClient);
        _discoverer = new HtmlLinkDiscoverer();
        _visitor = new InertVisitor();
    }

    public CrawlerBuilder WithFilter(ICrawlFilter filter)
    {
        _filters.Add(filter);
        
        return this;
    }

    public CrawlerBuilder WithFetcher(IFetcher fetcher)
    {
        _fetcher = fetcher;
        
        return this;
    }

    public CrawlerBuilder WithDiscoverer(ILinkDiscoverer discoverer)
    {
        _discoverer = discoverer;

        return this;
    }

    public CrawlerBuilder WithVisitor(ICrawlVisitor visitor)
    {
        _visitor = visitor;

        return this;
    }

    public Crawler Build()
    {
        ICrawlFilter filter = _filters.Count switch
        {
            0 => new InertFilter(),
            1 => _filters.First(),
            _ => new CompositeFilter(_filters.ToArray())
        };
        
        return new Crawler(
            filter,
            _fetcher,
            _discoverer,
            _visitor
        );
    }
}