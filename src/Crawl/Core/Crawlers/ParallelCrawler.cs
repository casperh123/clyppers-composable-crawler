using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.Core.Crawlers;

public class ParallelCrawler : Crawler
{
    private readonly BufferBlock<CrawlContext> _frontier;
    private readonly ActionBlock<CrawlContext> _worker;
    private readonly ConcurrentDictionary<string, byte> _seen;

    private long _totalCrawled;
    private int _pending;
    private IProgress<CrawlProgress>? _progress;

    public ParallelCrawler(
        ICrawlFilter filter,
        IFetcher fetcher,
        ILinkDiscoverer discoverer,
        ICrawlVisitor visitor,
        int parallelDegree = 4) : base(filter, fetcher, discoverer, visitor)
    {
        _seen = new ConcurrentDictionary<string, byte>();

        // Queue of URLs to crawl
        _frontier = new BufferBlock<CrawlContext>(new DataflowBlockOptions
        {
            // You can set BoundedCapacity if you want backpressure
            // BoundedCapacity = 1000
            
        });

        // Worker that processes each CrawlContext
        _worker = new ActionBlock<CrawlContext>(
            async context => await ProcessAndEnqueueLinks(context),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = parallelDegree,
                EnsureOrdered = false
            });

        // Frontier feeds the worker; when frontier completes, worker completes.
        _frontier.LinkTo(_worker, new DataflowLinkOptions
        {
            PropagateCompletion = true
        });
    }

    public override async Task CrawlAsync(
        Uri startUri,
        IProgress<CrawlProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _seen.Clear();
        _totalCrawled = 0;
        _pending = 0;
        _progress = progress;

        // Seed the crawl
        if (_seen.TryAdd(startUri.AbsoluteUri, 0))
        {
            Interlocked.Increment(ref _pending);

            await _frontier.SendAsync(new CrawlContext
            {
                Uri = startUri,
                ReferringUri = null,
                Depth = 0
            }, cancellationToken);
        }

        progress?.Report(CrawlProgress.Started());

        // Observe cancellation: when cancelled, stop accepting new items.
        using (cancellationToken.Register(() => _frontier.Complete()))
        {
            // Wait for the worker to finish all work
            await _worker.Completion;
        }
    }

    private async Task ProcessAndEnqueueLinks(CrawlContext context)
    {
        try
        {
            if (!Filter.ShouldCrawl(context))
            {
                return;
            }

            IEnumerable<CrawlContext> foundLinks = await ProcessUriAsync(context);

            foreach (CrawlContext foundLink in foundLinks)
            {
                // Dedup here: only enqueue URLs we haven't seen
                if (_seen.TryAdd(foundLink.Uri.AbsoluteUri, 0))
                {
                    Interlocked.Increment(ref _pending);
                    // Always enqueue into FRONTIER, not the worker directly
                    await _frontier.SendAsync(foundLink);
                }
            }

            int crawled = (int)Interlocked.Increment(ref _totalCrawled);
            _progress?.Report(CrawlProgress.Progress(context, crawled, _frontier.Count));
        }
        catch (Exception ex)
        {
            int crawled = (int)Interlocked.Read(ref _totalCrawled);
            _progress?.Report(CrawlProgress.Error(context, ex, crawled, _frontier.Count));
        }
        finally
        {
            // This context is fully processed (including enqueueing children)
            if (Interlocked.Decrement(ref _pending) == 0)
            {
                // No more items in the entire system → close the frontier
                // This will propagate completion to the worker via LinkTo.
                _frontier.Complete();
            }
        }
    }
}
