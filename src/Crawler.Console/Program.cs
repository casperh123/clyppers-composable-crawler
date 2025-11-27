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
    .WithVisitor(brokenLinkVisitor)
    .Build();

IProgress<CrawlProgress> progress = new Progress<CrawlProgress>(crawlProgress =>
{
    Console.WriteLine($"Crawling Url: {crawlProgress?.Context?.Uri}, Total crawled: {crawlProgress?.TotalCrawled}, Queue size: {crawlProgress?.QueueSize}");       
});
    
Uri uri = new Uri("https://trekantens-trailercenter.dk/");

await crawler.CrawlWebsiteAsync(uri, progress);

foreach (BrokenLinkReport brokenLink in brokenLinkVisitor.GetBrokenLinks())
{
    foreach (LinkReference link in brokenLink.References)
    {
        Console.WriteLine($"Link: {brokenLink.Url}, StatusCode: {brokenLink.StatusCode}, Referrer: {link.LinkedFrom}, Anchor Text: {link.AnchorText}, Line: {link.Line}");
    }
}