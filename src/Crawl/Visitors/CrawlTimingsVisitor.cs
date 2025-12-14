using System.Collections.Concurrent;
using AngleSharp.Html.Dom;
using Crawl.Core;
using Crawl.Models;

namespace Crawl.Visitors;

public class CrawlTimingsVisitor : ICrawlVisitor
{
    private readonly ConcurrentBag<CrawlTiming> _timings = [];
    
    public Task VisitAsync(CrawlResult result, IHtmlDocument? document, CancellationToken cancellationToken = default)
    {
        CrawlTiming timing = new CrawlTiming
        {
            Uri = result.Context.Uri,
            TTFB = result.FetchResult.TTFB,
            RequestDuration = result.FetchResult.RequestDuration,
            ElapsedTime = result.ElapsedTime
        };

        _timings.Add(timing);

        return Task.CompletedTask;
    }

    public ICollection<CrawlTiming> GetTimings()
    {
        return _timings.ToArray();
    }
}