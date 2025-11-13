using System.Diagnostics;
using System.Runtime.CompilerServices;
using Crawler.Models;

namespace Crawler.Core;

public class Crawler
{
    private ICrawlFilter _filter;
    private IFetcher _fetcher;
    private ILinkDiscoverer _discoverer;
    private ICrawlVisitor _visitor;

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
                IEnumerable<CrawlContext> foundLinks = await ProcessUriAsync(context);
                
                foreach(CrawlContext foundLink in foundLinks)
                {
                    queue.Enqueue(foundLink);
                }
            }
            catch (Exception ex)
            {
                progress?.Report(CrawlProgress.Error(context, ex, totalCrawled, queue.Count));
            }
            finally
            {
                totalCrawled += 1;
            }
        }

    }

    private async Task<IEnumerable<CrawlContext>> ProcessUriAsync(CrawlContext context)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        
        if (!_filter.ShouldCrawl(context))
        {
            return [];
        }

        FetchResult fetchResult = await _fetcher.FetchAsync(context);

        ICollection<DiscoveredLink> discoveredLinks = [];

        if (fetchResult.IsSuccess)
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