using AngleSharp.Html.Dom;
using Crawl.Core;
using Crawl.Core.Interfaces;
using Crawl.LinkDiscoverers;
using Crawl.Models;

namespace Crawl.Visitors;

public class LinkCollectorVisitor : ICrawlVisitor
{
    private readonly ILinkDiscoverer _linkDiscoverer;
    public ICollection<DiscoveredLink> _links;

    public LinkCollectorVisitor(ILinkDiscoverer? linkDiscoverer = null)
    {
        _linkDiscoverer = linkDiscoverer ?? new HtmlLinkDiscoverer();
        _links = [];
    }

    public Task VisitAsync(CrawlResult result, IHtmlDocument? document, CancellationToken cancellationToken = default)
    {
        ICollection<DiscoveredLink> discoveredLinks = _linkDiscoverer.DiscoverLinks(result.FetchResult, document, cancellationToken);
    
        foreach (DiscoveredLink link in discoveredLinks)
        {
            _links.Add(link);
        }

        return Task.CompletedTask;
    }
}