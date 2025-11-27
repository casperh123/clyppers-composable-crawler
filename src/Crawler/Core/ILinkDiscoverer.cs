using AngleSharp.Html.Dom;
using Crawler.Models;

namespace Crawler.Core;

public interface ILinkDiscoverer
{
    Task<ICollection<DiscoveredLink>> DiscoverLinks(FetchResult context, IHtmlDocument? document, CancellationToken cancellationToken = default);
}