using AngleSharp.Html.Dom;
using Crawl.Core;
using Crawl.Models;

namespace Crawl.Visitors;

public class InertVisitor : ICrawlVisitor
{
    public Task VisitAsync(CrawlResult result, IHtmlDocument? document, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}