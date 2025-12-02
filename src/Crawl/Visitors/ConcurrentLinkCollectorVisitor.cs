using System.Collections.Concurrent;
using AngleSharp.Html.Dom;
using Crawl.Core;
using Crawl.Core.Interfaces;
using Crawl.LinkDiscoverers;
using Crawl.Models;

namespace Crawl.Visitors;

public class ConcurrentLinkCollectorVisitor : ICrawlVisitor
{
    private readonly ILinkDiscoverer _linkDiscoverer;
    private readonly ConcurrentBag<DiscoveredLink> _links;

    public IEnumerable<DiscoveredLink> Links => _links;

    public ConcurrentLinkCollectorVisitor(ILinkDiscoverer? linkDiscoverer = null)
    {
        _linkDiscoverer = linkDiscoverer ?? new HtmlLinkDiscoverer();
        _links = new ConcurrentBag<DiscoveredLink>();
    }

    public Task VisitAsync(CrawlResult result, IHtmlDocument? document, CancellationToken cancellationToken = default)
    {
        foreach (DiscoveredLink link in _linkDiscoverer.DiscoverLinks(result.FetchResult, document, cancellationToken))
        {
            _links.Add(link);
        }

        return Task.CompletedTask;
    }
}