using Crawl.Core;
using Crawl.Core.Builder;
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

Crawler crawler = new SequentialCrawlerBuilder(client)
    .WithFilter(new SameHostFilter())
    .WithVisitor(brokenLinkVisitor)
    .Build();

IProgress<CrawlProgress> progress = new Progress<CrawlProgress>(crawlProgress =>
{
    Console.WriteLine($"Crawling Url: {crawlProgress?.Context?.Uri}, Total crawled: {crawlProgress?.TotalCrawled}, Queue size: {crawlProgress?.QueueSize}");       
});
    
Uri uri = new Uri("https://trekantens-trailercenter.dk/");

await crawler.CrawlAsync(uri, progress);