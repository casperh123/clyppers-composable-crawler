using Crawl.Core;
using Crawl.Core.Builders;
using Crawl.Filters;
using Crawl.Filters.ExclusionFilters;
using Crawl.Filters.InclusionFilters;
using Crawl.Models;
using Crawl.Visitors.BrokenLink;

HttpClientHandler handler = new HttpClientHandler
{
    AllowAutoRedirect = true,
    AutomaticDecompression = System.Net.DecompressionMethods.All,
};

HttpClient client = new HttpClient(handler);

// Set headers that mimic a normal desktop browser
client.DefaultRequestHeaders.UserAgent.ParseAdd(
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
    "AppleWebKit/537.36 (KHTML, like Gecko) " +
    "Chrome/123.0.0.0 Safari/537.36");

client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9,da;q=0.8");

/*
Crawl.Core.Crawl crawler = new CrawlerBuilder(client)
    .WithFilter(new SameHostFilter())
    .WithFilter(new DeduplicationFilter())
    .Build();
*/
BrokenLinkVisitor brokenLinkVisitor = new BrokenLinkVisitor();

Crawler crawler = new DomainParallelCrawlerBuilder(client)
    .WithParallelDegree(2)
    .WithWorkerCount(128)
    .WithFilter(new IncludeLTTsFilter("dk"))
    .WithFilter(new ExcludeFilesFilter())
    .WithFilter(new ExcludeImages())
    .Build();

IProgress<CrawlProgress> progress = new Progress<CrawlProgress>(crawlProgress =>
{
    Console.WriteLine($"Crawling Url: {crawlProgress?.Context?.Uri}, Total crawled: {crawlProgress?.TotalCrawled}, Queue size: {crawlProgress?.QueueSize}");       
});
    
Uri uri = new Uri("https://www.trekantens-trailercenter.dk");

await crawler.CrawlAsync(uri, progress);