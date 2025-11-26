using System.Diagnostics;
using Crawler.Models;

namespace Crawler.Core;

public class Crawler
{
    private readonly ICrawlFilter _filter;
    private readonly IFetcher _fetcher;
    private readonly ILinkDiscoverer _discoverer;
    private readonly ICrawlVisitor _visitor;

    public Crawler(ICrawlFilter filter, IFetcher fetcher, ILinkDiscoverer discoverer, ICrawlVisitor visitor)
    {
        _filter = filter;
        _fetcher = fetcher;
        _discoverer = discoverer;
        _visitor = visitor;
    }


    public async Task CrawlWebsiteAsync(
        Uri startUri,
        IProgress<CrawlProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Queue<CrawlContext> queue = new();
        int totalCrawled = 0;

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
                    queue.Enqueue(foundLink);
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

        using FetchResult fetchResult = await _fetcher.FetchAsync(context);

        ICollection<DiscoveredLink> discoveredLinks = [];

        if (fetchResult.Success)
        {
            discoveredLinks = await _discoverer.DiscoverLinks(fetchResult);
        }

        CrawlResult result = new CrawlResult
        {
            Context = context,
            FetchResult = fetchResult,
            DiscoveredLinks = discoveredLinks,
            ElapsedTime = stopwatch.Elapsed
        };

        await _visitor.VisitAsync(result);
        
        return discoveredLinks.Select(link => CrawlContext.FromDiscoveredLink(link, context.Depth + 1));
    }
}