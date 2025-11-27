using AngleSharp.Html.Dom;
using Crawler.Core;
using Crawler.Models;

namespace Crawler.Visitors;

public class InertVisitor : ICrawlVisitor
{
    public Task VisitAsync(CrawlResult result, IHtmlDocument? document, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}