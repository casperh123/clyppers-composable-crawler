using Crawl.Core.Crawlers;
using Crawl.Core.Interfaces;

namespace Crawl.Core.Builder;

public class SequentialCrawlerBuilder : AbstractCrawlerBuilder
{
    public SequentialCrawlerBuilder(HttpClient httpClient) : base(httpClient)
    {
    }

    public override Crawler Build()
    {
        ICrawlFilter filter = GetFilter();
        
        return new SequentialCrawler(
            filter,
            Fetcher,
            Discoverer,
            Visitor
        );
    }
}