using Crawl.Core;
using Crawl.Core.Builders;
using Crawl.Filters;
using Crawl.Filters.ExclusionFilters;
using Crawl.Filters.InclusionFilters;
using Crawl.Models;
using Crawl.Visitors;
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
CrawlTimingsVisitor crawlTimingsVisitor = new CrawlTimingsVisitor();
OneProviderVisitor visitor = new OneProviderVisitor();

Crawler crawler = new ParallelCrawlerBuilder(client)
    .WithParallelDegree(2)
    .WithFilter(new DepthFilter(1))
    .WithFilter(new SameHostFilter())
    .WithFilter(new ExcludeFilesFilter())
    .WithFilter(new ExcludeImages())
    .WithVisitor(visitor)
    .Build();

IProgress<CrawlProgress> progress = new Progress<CrawlProgress>(crawlProgress =>
{
    Console.WriteLine($"Crawling Url: {crawlProgress?.Context?.Uri}, Total crawled: {crawlProgress?.TotalCrawled}, Queue size: {crawlProgress?.QueueSize}");       
});
    
Uri uri = new Uri("https://oneprovider.com/");

await crawler.CrawlAsync(uri, progress);

foreach (CrawlTiming timing in crawlTimingsVisitor.GetTimings())
{
    Console.WriteLine($"URI: {timing.Uri}, Elapsed Time: {timing.ElapsedTime.Value.Milliseconds}, TTFB: {timing.TTFB.Value.Milliseconds}, Request Duration: {timing.RequestDuration.Value.Milliseconds}");
}

foreach (ServerInfo info in visitor.servers.OrderBy(server => server.PriceUsd))
{
    Console.WriteLine(info);
}