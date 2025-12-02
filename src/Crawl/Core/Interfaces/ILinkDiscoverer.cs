using AngleSharp.Html.Dom;
using Crawl.Models;

namespace Crawl.Core.Interfaces;

public interface ILinkDiscoverer
{
    ICollection<DiscoveredLink> DiscoverLinks(FetchResult context, IHtmlDocument? document, CancellationToken cancellationToken = default);
}