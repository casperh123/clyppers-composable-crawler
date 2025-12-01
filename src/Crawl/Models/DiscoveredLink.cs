namespace Crawl.Models;

public record DiscoveredLink
{
    public required Uri Uri { get; init; }
    public int? Line { get; init; }
    public string? AnchorText { get; init; }
}