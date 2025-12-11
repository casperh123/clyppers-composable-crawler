using System.Threading.Tasks.Dataflow;
using Crawl.Core.Builders;
using Crawl.Core.Interfaces;
using Crawl.Filters;
using Crawl.Models;
using Crawl.Visitors;

namespace Crawl.Core.Crawlers.DomainParallel;

public class DomainParallelCrawler : Crawler
{
    private readonly SeenSet _crawledDomains;
    private readonly HttpClient _httpClient;
    private readonly int _maxPerDomain;
    private readonly ActionBlock<string> _crawlProcessor;
    private int _pendingWork;
    private IProgress<CrawlProgress>? _progress;
    private CancellationToken _cancellationToken;

    public DomainParallelCrawler(
        ICrawlFilter filter,
        IFetcher fetcher,
        ILinkDiscoverer discoverer,
        ICrawlVisitor visitor,
        HttpClient httpClient,
        int maxPerDomain = 2,
        int workerCount = 16)
        : base(filter, fetcher, discoverer, visitor)
    {
        _maxPerDomain = maxPerDomain;
        _httpClient = httpClient;
        _crawledDomains = new SeenSet(100_000);
        _pendingWork = 0;

        _crawlProcessor = new ActionBlock<string>(
            async host =>
            {
                try
                {
                    await CrawlDomainAsync(host, _progress, _cancellationToken);
                }
                finally
                {
                    if (Interlocked.Decrement(ref _pendingWork) == 0)
                    {
                        _crawlProcessor.Complete();
                    }
                }
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = workerCount,
                EnsureOrdered = false
            }
        );
    }

    public override async Task CrawlAsync(
        Uri startUri,
        IProgress<CrawlProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _progress = progress;
        _cancellationToken = cancellationToken;

        _crawledDomains.TryAdd(startUri.Host);
        Interlocked.Increment(ref _pendingWork);
        await _crawlProcessor.SendAsync(startUri.Host, cancellationToken);

        await _crawlProcessor.Completion;
    }

    private async Task CrawlDomainAsync(
        string host,
        IProgress<CrawlProgress>? progress,
        CancellationToken cancellationToken)
    {
        Uri uri = new Uri("https://" + host);

        ConcurrentLinkCollectorVisitor linkCollector = new ConcurrentLinkCollectorVisitor();

        Crawler crawler = new ParallelCrawlerBuilder(_httpClient)
            .WithParallelDegree(_maxPerDomain)
            .WithFilters(new SameHostFilter(), Filter)
            .WithDiscoverer(Discoverer)
            .WithFetcher(Fetcher)
            .WithVisitors(Visitor, linkCollector)
            .Build();

        await crawler.CrawlAsync(uri, progress, cancellationToken);

        // Collect all new domains that haven't been crawled yet
        List<string> newDomains = linkCollector.Links
            .Select(discovered => discovered.Uri.Host)
            .Where(newHost => _crawledDomains.TryAdd(newHost))
            .ToList();

        // Increment pending work for all new domains at once, BEFORE the finally block decrements
        if (newDomains.Count > 0)
        {
            Interlocked.Add(ref _pendingWork, newDomains.Count);
            
            foreach (string newHost in newDomains)
            {
                await _crawlProcessor.SendAsync(newHost, cancellationToken);
            }
        }
    }
}