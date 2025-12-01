using AngleSharp.Html.Dom;
using Crawl.Models;

namespace Crawl.Core;

public interface ILinkDiscoverer
{
    Task<ICollection<DiscoveredLink>> DiscoverLinks(FetchResult context, IHtmlDocument? document, CancellationToken cancellationToken = default);
}