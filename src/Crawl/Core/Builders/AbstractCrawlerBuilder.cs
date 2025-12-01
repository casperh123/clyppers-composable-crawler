using Crawl.Core.Interfaces;
using Crawl.Fetchers;
using Crawl.Filters;
using Crawl.LinkDiscoverers;
using Crawl.Visitors;

namespace Crawl.Core.Builder;

public abstract class AbstractCrawlerBuilder 
{
    private readonly IList<ICrawlFilter> _filters = [];
    protected IFetcher Fetcher;
    protected ILinkDiscoverer Discoverer;
    protected ICrawlVisitor Visitor;

    public AbstractCrawlerBuilder(HttpClient httpClient)
    {
        Fetcher = new HttpFetcher(httpClient);
        Discoverer = new HtmlLinkDiscoverer();
        Visitor = new InertVisitor();
    }

    public AbstractCrawlerBuilder WithFilter(ICrawlFilter filter)
    {
        _filters.Add(filter);
        
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
        Visitor = visitor;

        return this;
    }

    public ICrawlFilter GetFilter()
    {
       return _filters.Count switch
        {
            0 => new InertFilter(),
            1 => _filters.First(),
            _ => new CompositeFilter(_filters.ToArray())
        };
    }

    public abstract Crawler Build();
}