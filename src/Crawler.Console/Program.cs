using System.Runtime.CompilerServices;
using Crawler.Core;
using Crawler.Filters;
using Crawler.Models;
using Crawler.Visitors.BrokenLink;

HttpClient client = new HttpClient();

/*
Crawler.Core.Crawler crawler = new CrawlerBuilder(client)
    .WithFilter(new SameHostFilter())
    .WithFilter(new DeduplicationFilter())
    .Build();
*/
BrokenLinkVisitor brokenLinkVisitor = new BrokenLinkVisitor();

Crawler.Core.Crawler crawler = new CrawlerBuilder(client)
    .WithFilter(new SameHostFilter())
    .WithVisitor(new BrokenLinkVisitor())
    .Build();

IProgress<CrawlProgress> progress = new Progress<CrawlProgress>(crawlProgress =>
{
    Console.WriteLine($"Crawling Url: {crawlProgress?.Context?.Uri}, Total crawled: {crawlProgress?.TotalCrawled}, Queue size: {crawlProgress?.QueueSize}");       
});
    
Uri uri = new Uri("https://tuholaiskauppa.fi/");

await crawler.CrawlWebsiteAsync(uri, progress);

foreach (BrokenLinkReport brokenLink in brokenLinkVisitor.GetBrokenLinks())
{
    foreach ((Uri referrer, DiscoveredLink link) in brokenLink.References)
    {
        Console.WriteLine($"Link: {brokenLink.Uri}, StatusCode: {brokenLink.StatusCode}, Referrer: {referrer.ToString()}, Anchor Text: {link.AnchorText}, Line: {link.Line}");
    }
}