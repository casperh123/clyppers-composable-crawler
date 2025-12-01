using System.Threading.Channels;
using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.Core.Crawlers.DomainParallel;

public class DomainParallelCrawler : Crawler
{
    private readonly DomainScheduler _scheduler;
    private readonly SeenSet _seen; // now sharded
    private readonly Channel<CrawlContext> _workChannel;

    private readonly int _workerCount;
    private long _totalCrawled;
    private int _pending;
    private IProgress<CrawlProgress>? _progress;
    private CancellationToken _token;

    public DomainParallelCrawler(
        ICrawlFilter filter,
        IFetcher fetcher,
        ILinkDiscoverer discoverer,
        ICrawlVisitor visitor,
        int maxPerDomain = 2,
        int workerCount = 16)
        : base(filter, fetcher, discoverer, visitor)
    {
        _workerCount = workerCount;
        _scheduler = new DomainScheduler(maxPerDomain);
        _seen = new SeenSet(10_000_000);

        _workChannel = Channel.CreateBounded<CrawlContext>(new BoundedChannelOptions(20000)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public override async Task CrawlAsync(
        Uri startUri,
        IProgress<CrawlProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _progress = progress;
        _token = cancellationToken;
        _scheduler.Clear();
        _totalCrawled = 0;
        _pending = 0;

        if (_seen.TryAdd(startUri.AbsoluteUri))
        {
            _scheduler.Enqueue(startUri, new CrawlContext(startUri));
            Interlocked.Increment(ref _pending);
        }

        progress?.Report(CrawlProgress.Started());

        Task schedulerTask = Task.Run(() => _scheduler.RunScheduler(_workChannel.Writer, _token), cancellationToken);
        Task[] workers = Enumerable.Range(0, _workerCount)
            .Select(_ => Task.Run(WorkerLoop, cancellationToken))
            .ToArray();

        await Task.WhenAll(workers.Append(schedulerTask));
    }

    private async ValueTask WorkerLoop()
    {
        ChannelReader<CrawlContext> reader = _workChannel.Reader;

        while (await reader.WaitToReadAsync(_token))
        {
            if (!reader.TryRead(out CrawlContext context))
                continue;

            try
            {
                await ProcessAndEnqueueLinks(context);
            }
            finally
            {
                _scheduler.DecrementInFlight(context.Uri.Host);
                if (Interlocked.Decrement(ref _pending) == 0)
                    _workChannel.Writer.TryComplete();
            }
        }
    }

    private async ValueTask ProcessAndEnqueueLinks(CrawlContext context)
    {
        try
        {
            if (!Filter.ShouldCrawl(context))
                return;

            IEnumerable<CrawlContext> found = await ProcessUriAsync(context);

            foreach (CrawlContext child in found)
            {
                if (_seen.TryAdd(child.Uri.AbsoluteUri))
                {
                    _scheduler.Enqueue(child.Uri, child);
                    Interlocked.Increment(ref _pending);
                }
            }

            int crawled = (int)Interlocked.Increment(ref _totalCrawled);
            _progress?.Report(CrawlProgress.Progress(context, crawled, _pending));
        }
        catch (Exception ex)
        {
            int crawled = (int)Interlocked.Read(ref _totalCrawled);
            _progress?.Report(CrawlProgress.Error(context, ex, crawled, _pending));
        }
    }
}
