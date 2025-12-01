using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.Core.Crawlers;

public class DomainParallelCrawler : Crawler
{
    private readonly BufferBlock<CrawlContext> _frontier;
    private readonly ActionBlock<CrawlContext> _dispatchBlock;

    private readonly ConcurrentDictionary<string, ActionBlock<CrawlContext>> _domainWorkers;
    private readonly ConcurrentDictionary<string, byte> _seen;

    private readonly int _maxPerDomain;
    private long _totalCrawled;
    private int _pending;
    private IProgress<CrawlProgress>? _progress;

    public DomainParallelCrawler(
        ICrawlFilter filter,
        IFetcher fetcher,
        ILinkDiscoverer discoverer,
        ICrawlVisitor visitor,
        int maxPerDomain = 2) : base(filter, fetcher, discoverer, visitor)
    {
        _maxPerDomain = maxPerDomain;

        _seen = new ConcurrentDictionary<string, byte>();
        _domainWorkers = new ConcurrentDictionary<string, ActionBlock<CrawlContext>>();

        // Global frontier queue
        _frontier = new BufferBlock<CrawlContext>(
            new DataflowBlockOptions
            {
                // Optionally: BoundedCapacity = 5000
            });

        // Dispatcher: routes contexts to per-domain blocks
        _dispatchBlock = new ActionBlock<CrawlContext>(
            context =>
            {
                string host = context.Uri.Host.ToLowerInvariant();
                ActionBlock<CrawlContext> domainBlock =
                    _domainWorkers.GetOrAdd(host, CreateDomainWorker);

                domainBlock.Post(context);
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount * 32,
                EnsureOrdered = false
            });

        _frontier.LinkTo(_dispatchBlock, new DataflowLinkOptions
        {
            PropagateCompletion = true
        });
    }

    private ActionBlock<CrawlContext> CreateDomainWorker(string host)
    {
        return new ActionBlock<CrawlContext>(
            async context => await ProcessAndEnqueueLinks(context),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = _maxPerDomain, // per-domain throttle
                EnsureOrdered = false
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
        _domainWorkers.Clear();

        // Seed
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

        await using (cancellationToken.Register(() => _frontier.Complete()))
        {
            // Wait for dispatcher to finish
            await _dispatchBlock.Completion;

            // Now wait for all domain workers
            foreach (var block in _domainWorkers.Values)
                block.Complete();

            foreach (var block in _domainWorkers.Values)
                await block.Completion;
        }
    }

    private async Task ProcessAndEnqueueLinks(CrawlContext context)
    {
        try
        {
            if (!Filter.ShouldCrawl(context))
                return;

            IEnumerable<CrawlContext> found = await ProcessUriAsync(context);

            foreach (CrawlContext child in found)
            {
                if (_seen.TryAdd(child.Uri.AbsoluteUri, 0))
                {
                    Interlocked.Increment(ref _pending);
                    await _frontier.SendAsync(child);
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
            if (Interlocked.Decrement(ref _pending) == 0)
                _frontier.Complete();
        }
    }
}
