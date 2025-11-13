using Crawler.Models;

namespace Crawler.Core;

public interface ILinkDiscoverer
{
    Task<ICollection<DiscoveredLink>> DiscoverLinks(FetchResult context, CancellationToken cancellationToken = default);
}