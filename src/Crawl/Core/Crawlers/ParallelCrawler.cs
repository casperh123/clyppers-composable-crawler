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

        _frontier = new BufferBlock<CrawlContext>();

        _worker = new ActionBlock<CrawlContext>(
            async context => await ProcessAndEnqueueLinks(context),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = parallelDegree,
                EnsureOrdered = false
            });

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

        if (_seen.TryAdd(startUri.AbsoluteUri, 0))
        {
            Interlocked.Increment(ref _pending);

            await _frontier.SendAsync(new CrawlContext(startUri), cancellationToken);
        }

        progress?.Report(CrawlProgress.Started());

        await using (cancellationToken.Register(() => _frontier.Complete()))
        {
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
                if (_seen.TryAdd(foundLink.Uri.AbsoluteUri, 0))
                {
                    Interlocked.Increment(ref _pending);
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
            if (Interlocked.Decrement(ref _pending) == 0)
            {
                _frontier.Complete();
            }
        }
    }
}
