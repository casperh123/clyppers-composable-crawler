using AngleSharp.Html.Dom;
using Crawl.Models;

namespace Crawl.Core;

public interface ICrawlVisitor
{
    Task VisitAsync(CrawlResult result, IHtmlDocument? document, CancellationToken cancellationToken = default);
}