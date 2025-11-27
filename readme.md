# Crawler

A lightweight, composable web crawler for .NET.

## Design Philosophy

Crawler is built around four simple interfaces: fetch, filter, discover, visit. Each does one thing. You compose them to build the crawler you need.

No inheritance hierarchies. No configuration objects with 50 properties. No magic.

The crawler visits each unique URL once, extracts links, and lets your visitor process the results. That's it.

## Quick Start
```csharp
var crawler = new CrawlerBuilder(new HttpClient())
    .WithFilter(new SameHostFilter())
    .Build();

await crawler.CrawlWebsiteAsync(new Uri("https://example.com"));
```

## Broken Link Detection
```csharp
var visitor = new BrokenLinkVisitor();

var crawler = new CrawlerBuilder(new HttpClient())
    .WithFilter(new SameHostFilter())
    .WithVisitor(visitor)
    .Build();

await crawler.CrawlWebsiteAsync(new Uri("https://example.com"));

foreach (var broken in visitor.GetBrokenLinks())
{
    Console.WriteLine($"{broken.Uri} ({broken.StatusCode})");
    
    foreach (var (referrer, link) in broken.References)
    {
        Console.WriteLine($"  Linked from: {referrer} (line {link.Line})");
    }
}
```

## Progress Reporting
```csharp
var progress = new Progress<CrawlProgress>(p =>
{
    Console.WriteLine($"Crawled: {p.Context?.Uri} ({p.TotalCrawled} total, {p.QueueSize} queued)");
});

await crawler.CrawlWebsiteAsync(uri, progress);
```

## Components

| Interface | Purpose | Built-in |
|-----------|---------|----------|
| `ICrawlFilter` | Decides which URLs to crawl | `SameHostFilter`, `InertFilter` |
| `ICrawlVisitor` | Processes crawled pages | `BrokenLinkVisitor`, `InertVisitor` |
| `IFetcher` | Fetches page content | `HttpFetcher` |
| `ILinkDiscoverer` | Extracts links from pages | `HtmlLinkDiscoverer` |

## Custom Filter Example
```csharp
public class MaxDepthFilter : ICrawlFilter
{
    private readonly int _maxDepth;

    public MaxDepthFilter(int maxDepth) => _maxDepth = maxDepth;

    public bool ShouldCrawl(CrawlContext context) => context.Depth <= _maxDepth;
}
```

## Custom Visitor Example
```csharp
public class PageCountVisitor : ICrawlVisitor
{
    public int Count { get; private set; }

    public Task VisitAsync(CrawlResult result, IHtmlDocument? document, CancellationToken ct = default)
    {
        Count++;
        return Task.CompletedTask;
    }
}
```

## License

MIT