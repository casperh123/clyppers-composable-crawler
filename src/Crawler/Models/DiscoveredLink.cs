namespace Crawler.Models;

public record DiscoveredLink
{
    public Uri Uri { get; set; }
    public Uri ReferringUri { get; set; }
}