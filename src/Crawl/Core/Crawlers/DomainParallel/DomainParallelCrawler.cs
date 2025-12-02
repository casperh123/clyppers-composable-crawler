using System.Threading.Channels;
using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.Core.Crawlers.DomainParallel;

public class DomainParallelCrawler : Crawler
{
    private readonly DomainScheduler _scheduler;
    private readonly SeenSet _seen;
    private readonly Channel<CrawlContext> _work;

    private readonly int _workerCount;

    private long _totalCrawled;
    private long _active;

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

        _work = Channel.CreateUnbounded<CrawlContext>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = false,
            AllowSynchronousContinuations = false
        });
    }

    public override async Task CrawlAsync(
        Uri startUri,
        IProgress<CrawlProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _progress = progress;
        _token = cancellationToken;

        _seen.Clear();
        _scheduler.Clear();

        _totalCrawled = 0;
        _active = 0;

        progress?.Report(CrawlProgress.Started());

        string startNorm = NormalizeUrl(startUri.AbsoluteUri);

        if (_seen.TryAdd(startNorm)
            && Filter.ShouldCrawl(new CrawlContext(startUri)))
        {
            _scheduler.Enqueue(startUri, new CrawlContext(startUri));
        }

        var schedulerTask = _scheduler.RunAsync(_work.Writer, _token);
        var workers = Enumerable.Range(0, _workerCount).Select(_ => WorkerLoop()).ToArray();

        await schedulerTask;
        _work.Writer.TryComplete();
        await Task.WhenAll(workers);


        progress?.Report(CrawlProgress.Completed((int)_totalCrawled));
    }

    private async Task WorkerLoop()
    {
        var reader = _work.Reader;

        while (await reader.WaitToReadAsync(_token))
        {
            while (reader.TryRead(out var ctx))
            {
                Interlocked.Increment(ref _active);

                try
                {
                    await ProcessAndDiscover(ctx);
                }
                finally
                {
                    _scheduler.DecrementInFlight(ctx.Uri.Host);
                    Interlocked.Decrement(ref _active);
                }
            }
        }
    }

    private async Task ProcessAndDiscover(CrawlContext ctx)
    {
        try
        {
            IEnumerable<CrawlContext> children = await ProcessUriAsync(ctx);

            foreach (var child in children)
            {
                string norm = NormalizeUrl(child.Uri.AbsoluteUri);

                if (!_seen.TryAdd(norm))
                    continue;

                if (!Filter.ShouldCrawl(child))
                    continue;

                _scheduler.Enqueue(child.Uri, child);
            }

            long crawled = Interlocked.Increment(ref _totalCrawled);
            _progress?.Report(CrawlProgress.Progress(ctx, (int)crawled, (int)_active));
        }
        catch (Exception ex)
        {
            long crawled = Interlocked.Read(ref _totalCrawled);
            _progress?.Report(CrawlProgress.Error(ctx, ex, (int)crawled, (int)_active));
        }
    }

    private static string NormalizeUrl(string url)
        => url.Trim().ToLowerInvariant();
}
