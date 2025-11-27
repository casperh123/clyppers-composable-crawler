using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Crawler.Core;
using Crawler.Models;

namespace Crawler.LinkDiscoverers;

public class HtmlLinkDiscoverer : ILinkDiscoverer
{
    public async Task<ICollection<DiscoveredLink>> DiscoverLinks(
        FetchResult fetchResult, 
        IHtmlDocument? document, 
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            return [];
        }

        var links = document.Links
            .Select(el => ToDiscoveredLink(el, fetchResult.Uri))
            .OfType<DiscoveredLink>()
            .ToList();

        return links;
    }

    private static DiscoveredLink? ToDiscoveredLink(IElement element, Uri baseUri)
    {
        var href = element.GetAttribute("href");
        
        if (string.IsNullOrWhiteSpace(href)) return null;
        if (!Uri.TryCreate(baseUri, href, out var uri)) return null;
        if (!IsValidUri(uri)) return null;

        return new DiscoveredLink
        {
            Uri = uri,
            AnchorText = element.TextContent.Trim(),
            Line = element.SourceReference?.Position.Line
        };
    }

    private static bool IsValidUri(Uri uri) => uri switch
    {
        { Scheme: not ("http" or "https") } => false,
        { Query.Length: > 0 } => false,
        { Fragment.Length: > 0 } => false,
        _ => true
    };
}