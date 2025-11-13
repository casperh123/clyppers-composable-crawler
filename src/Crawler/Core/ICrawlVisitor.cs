using Crawler.Models;

namespace Crawler.Core;

public interface ICrawlVisitor
{
    Task VisitAsync(CrawlResult result, CancellationToken cancellationToken = default);
}