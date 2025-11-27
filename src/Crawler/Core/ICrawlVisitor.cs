using AngleSharp.Html.Dom;
using Crawler.Models;

namespace Crawler.Core;

public interface ICrawlVisitor
{
    Task VisitAsync(CrawlResult result, IHtmlDocument? document, CancellationToken cancellationToken = default);
}