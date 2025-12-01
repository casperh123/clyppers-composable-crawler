using System.Diagnostics;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.Core;

public abstract class Crawler
{
    protected readonly ICrawlFilter Filter;
    protected readonly IFetcher Fetcher;
    protected readonly ILinkDiscoverer Discoverer;
    protected readonly ICrawlVisitor Visitor;
    private readonly HtmlParser _htmlParser;

    public Crawler(ICrawlFilter filter, IFetcher fetcher, ILinkDiscoverer discoverer, ICrawlVisitor visitor)
    {
        Filter = filter;
        Fetcher = fetcher;
        Discoverer = discoverer;
        Visitor = visitor;
        _htmlParser = new HtmlParser(new HtmlParserOptions
        {
            IsKeepingSourceReferences = true
        });
    }
    
    public abstract Task CrawlAsync(Uri startUri, IProgress<CrawlProgress>? progress = null, CancellationToken cancellationToken = default);

    protected async Task<IEnumerable<CrawlContext>> ProcessUriAsync(CrawlContext context)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        FetchResult fetchResult = await Fetcher.FetchAsync(context);
        ICollection<DiscoveredLink> discoveredLinks = [];
        IHtmlDocument? document = null;

        if (fetchResult is { Success: true, Content: not null }
            && fetchResult.ContentType?.Contains("html") == true)
        {
            document = await _htmlParser.ParseDocumentAsync(fetchResult.Content);
            discoveredLinks = await Discoverer.DiscoverLinks(fetchResult, document);
        }

        CrawlResult result = new CrawlResult
        {
            Context = context,
            FetchResult = fetchResult,
            DiscoveredLinks = discoveredLinks,
            ElapsedTime = stopwatch.Elapsed
        };

        await Visitor.VisitAsync(result, document);
        
        return discoveredLinks.Select(link => CrawlContext.From(link.Uri, context.Uri, context.Depth + 1));
    }
}