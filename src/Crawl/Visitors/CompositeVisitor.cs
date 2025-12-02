using AngleSharp.Html.Dom;
using Crawl.Core;
using Crawl.Models;

namespace Crawl.Visitors;

public class CompositeVisitor : ICrawlVisitor
{
    private readonly ICollection<ICrawlVisitor> _visitors;

    public CompositeVisitor(params ICrawlVisitor[] visitors)
    {
        _visitors = visitors;
    }
    public async Task VisitAsync(CrawlResult result, IHtmlDocument? document, CancellationToken cancellationToken = default)
    {
        IEnumerable<Task> tasks = _visitors
            .Select(v => v.VisitAsync(result, document, cancellationToken));

        await Task.WhenAll(tasks);
    }

}