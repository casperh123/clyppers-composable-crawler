using Crawl.Core.Builder;
using Crawl.Core.Crawlers;
using Crawl.Core.Interfaces;

namespace Crawl.Core.Builders;

public class ParallelCrawlerBuilder : AbstractCrawlerBuilder
{
    private int _parallelDegree = 100;
    
    public ParallelCrawlerBuilder(HttpClient httpClient) : base(httpClient)
    {
    }

    public ParallelCrawlerBuilder WithParallelDegree(int parallelDegree)
    {
        _parallelDegree = parallelDegree;

        return this;
    }

    public override Crawler Build()
    {
        ICrawlFilter filter = GetFilter();

        return new ParallelCrawler(
            filter,
            Fetcher,
            Discoverer,
            Visitor,
            _parallelDegree
        );
    }
}