using Crawler.Core;
using Crawler.Filters;
using Crawler.Models;

HttpClient client = new HttpClient();

Crawler.Core.Crawler crawler = new CrawlerBuilder(client)
    .WithFilter(new SameHostFilter())
    .WithFilter(new DeduplicationFilter())
    .Build();

IProgress<CrawlProgress> progress = new Progress<CrawlProgress>(crawlProgress =>
{
    Console.WriteLine($"Crawling Url: {crawlProgress?.Context?.Uri}, Total crawled: {crawlProgress?.TotalCrawled}, Queue size: {crawlProgress?.QueueSize}");       
});
    
Uri uri = new Uri("https://skadedyrsexperten.dk");

await crawler.CrawlWebsiteAsync(uri, progress);