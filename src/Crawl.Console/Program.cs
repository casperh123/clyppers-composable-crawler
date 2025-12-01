using Crawl.Core;
using Crawl.Core.Builder;
using Crawl.Core.Builders;
using Crawl.Filters;
using Crawl.Models;
using Crawl.Visitors.BrokenLink;

HttpClient client = new HttpClient();

/*
Crawl.Core.Crawl crawler = new CrawlerBuilder(client)
    .WithFilter(new SameHostFilter())
    .WithFilter(new DeduplicationFilter())
    .Build();
*/
BrokenLinkVisitor brokenLinkVisitor = new BrokenLinkVisitor();

Crawler crawler = new DomainParallelCrawlerBuilder(client)
    .WithFilter(new IncludeLTTsFilter("dk"))
    .Build();

IProgress<CrawlProgress> progress = new Progress<CrawlProgress>(crawlProgress =>
{
    Console.WriteLine($"Crawling Url: {crawlProgress?.Context?.Uri}, Total crawled: {crawlProgress?.TotalCrawled}, Queue size: {crawlProgress?.QueueSize}");       
});
    
Uri uri = new Uri("https://skadedyrsexperten.dk/");

await crawler.CrawlAsync(uri, progress);