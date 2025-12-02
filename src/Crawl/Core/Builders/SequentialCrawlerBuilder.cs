using Crawl.Core.Crawlers;
using Crawl.Core.Interfaces;

namespace Crawl.Core.Builders;

public class SequentialCrawlerBuilder : AbstractCrawlerBuilder
{
    public SequentialCrawlerBuilder(HttpClient httpClient) : base(httpClient)
    {
    }

    public override Crawler Build()
    {
        ICrawlFilter filter = GetFilter();
        ICrawlVisitor visitor = GetVisitor();
        
        return new SequentialCrawler(
            filter,
            Fetcher,
            Discoverer,
            visitor
        );
    }
}