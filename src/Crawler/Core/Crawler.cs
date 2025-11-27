using System.Diagnostics;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Crawler.Models;

namespace Crawler.Core;

public class Crawler
{
    private readonly ICrawlFilter _filter;
    private readonly IFetcher _fetcher;
    private readonly ILinkDiscoverer _discoverer;
    private readonly ICrawlVisitor _visitor;
    private readonly HtmlParser _htmlParser;

    public Crawler(ICrawlFilter filter, IFetcher fetcher, ILinkDiscoverer discoverer, ICrawlVisitor visitor)
    {
        _filter = filter;
        _fetcher = fetcher;
        _discoverer = discoverer;
        _visitor = visitor;
        _htmlParser = new HtmlParser(new HtmlParserOptions
        {
            IsKeepingSourceReferences = true
        });
    }


    public async Task CrawlWebsiteAsync(
        Uri startUri,
        IProgress<CrawlProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        HashSet<string> seen = [];
        Queue<CrawlContext> queue = new();
        int totalCrawled = 0;

        seen.Add(startUri.AbsoluteUri);
        queue.Enqueue(new CrawlContext
        {
            Uri = startUri,
            ReferringUri = null,
            Depth = 0
        });

        progress?.Report(CrawlProgress.Started());

        while (queue.TryDequeue(out CrawlContext? context) && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!_filter.ShouldCrawl(context))
                {
                    continue;
                }
                
                IEnumerable<CrawlContext> foundLinks = await ProcessUriAsync(context);
                
                foreach(CrawlContext foundLink in foundLinks)
                {
                    if (seen.Add(foundLink.Uri.AbsoluteUri))
                    {
                        queue.Enqueue(foundLink);
                    }                
                }
                
                totalCrawled += 1;
                
                progress?.Report(CrawlProgress.Progress(context, totalCrawled, queue.Count));
            }
            catch (Exception ex)
            {
                progress?.Report(CrawlProgress.Error(context, ex, totalCrawled, queue.Count));
            }
        }

    }

    private async Task<IEnumerable<CrawlContext>> ProcessUriAsync(CrawlContext context)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        FetchResult fetchResult = await _fetcher.FetchAsync(context);
        ICollection<DiscoveredLink> discoveredLinks = [];
        IHtmlDocument? document = null;

        if (fetchResult.Success && 
            fetchResult.Content is not null
            && fetchResult.ContentType?.Contains("html") == true)
        {
            document = await _htmlParser.ParseDocumentAsync(fetchResult.Content);
            discoveredLinks = await _discoverer.DiscoverLinks(fetchResult, document);
        }

        CrawlResult result = new CrawlResult
        {
            Context = context,
            FetchResult = fetchResult,
            DiscoveredLinks = discoveredLinks,
            ElapsedTime = stopwatch.Elapsed
        };

        await _visitor.VisitAsync(result, document);
        
        return discoveredLinks.Select(link => CrawlContext.From(link.Uri, context.Uri, context.Depth + 1));
    }
}