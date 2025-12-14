using Crawl.Core.Interfaces;
using Crawl.Fetchers;
using Crawl.Filters;
using Crawl.LinkDiscoverers;
using Crawl.Visitors;

namespace Crawl.Core.Builders;

public abstract class AbstractCrawlerBuilder 
{
    private readonly IList<ICrawlFilter> _filters = [];
    private readonly IList<ICrawlVisitor> _visitors = [];
    protected IFetcher Fetcher;
    protected ILinkDiscoverer Discoverer;
    protected HttpClient HttpClient;

    public AbstractCrawlerBuilder(HttpClient httpClient)
    {
        Fetcher = new HttpFetcher(httpClient);
        Discoverer = new HtmlLinkDiscoverer();
        HttpClient = httpClient;
    }

    public AbstractCrawlerBuilder WithFilter(ICrawlFilter filter)
    {
        _filters.Add(filter);
        
        return this;
    }

    public AbstractCrawlerBuilder WithFilters(params ICrawlFilter[] filter)
    {
        foreach (ICrawlFilter crawlFilter in filter)
        {
            _filters.Add(crawlFilter);
        }

        return this;
    }

    public AbstractCrawlerBuilder WithFetcher(IFetcher fetcher)
    {
        Fetcher = fetcher;
        
        return this;
    }

    public AbstractCrawlerBuilder WithDiscoverer(ILinkDiscoverer discoverer)
    {
        Discoverer = discoverer;

        return this;
    }

    public AbstractCrawlerBuilder WithVisitor(ICrawlVisitor visitor)
    {
        _visitors.Add(visitor);

        return this;
    }

    public AbstractCrawlerBuilder WithVisitors(params ICrawlVisitor[] visitors)
    {
        foreach (ICrawlVisitor visitor in visitors)
        {
            _visitors.Add(visitor);
        }

        return this;
    }

    protected ICrawlFilter GetFilter()
    {
       return _filters.Count switch
        {
            0 => new InertFilter(),
            1 => _filters.First(),
            _ => new CompositeFilter(_filters.ToArray())
        };
    }

    protected ICrawlVisitor GetVisitor()
    {
        return _visitors.Count switch
        {
            0 => new InertVisitor(),
            1 => _visitors.First(),
            _ => new CompositeVisitor(_visitors.ToArray())
        };
    }

    public abstract Crawler Build();
}