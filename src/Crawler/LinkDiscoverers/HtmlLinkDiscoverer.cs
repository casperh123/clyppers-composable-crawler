using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Crawler.Core;
using Crawler.Models;

namespace Crawler.LinkDiscoverers;

public class HtmlLinkDiscoverer : ILinkDiscoverer
{
    public Task<ICollection<DiscoveredLink>> DiscoverLinks(
        FetchResult fetchResult, 
        IHtmlDocument? document, 
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            return Task.FromResult<ICollection<DiscoveredLink>>([]);
        }

        ICollection<DiscoveredLink> links = [];

        foreach (IElement link in document.Links)
        {
            string? href = link.GetAttribute("href");
            
            if (string.IsNullOrWhiteSpace(href)) continue;
            if (!Uri.TryCreate(fetchResult.Uri, href, out Uri? resolvedUri)) continue;
            
            links.Add(new DiscoveredLink
            {
                Uri = resolvedUri,
                AnchorText = link.TextContent.Trim(),
                Line = link.SourceReference?.Position.Line ?? -1
            });
        }

        return Task.FromResult(links);
    }
}