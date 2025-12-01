using Crawl.Core.Builder;
using Crawl.Core.Crawlers.DomainParallel;
using Crawl.Core.Interfaces;

namespace Crawl.Core.Builders;

public class DomainParallelCrawlerBuilder : AbstractCrawlerBuilder
{
    private int _parallelDegree = 1;
    private int _workerCount = 16;
    
    public DomainParallelCrawlerBuilder(HttpClient httpClient) : base(httpClient)
    {
    }

    public DomainParallelCrawlerBuilder WithParallelDegree(int parallelDegree)
    {
        _parallelDegree = parallelDegree;

        return this;
    }

    public DomainParallelCrawlerBuilder WithWorkerCount(int workerCount)
    {
        _workerCount = workerCount;

        return this;
    }

    public override Crawler Build()
    {
        ICrawlFilter filter = GetFilter();

        return new DomainParallelCrawler(
            filter,
            Fetcher,
            Discoverer,
            Visitor,
            _parallelDegree
            ,_workerCount
        );
    }
}