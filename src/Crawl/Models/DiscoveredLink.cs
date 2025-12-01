namespace Crawl.Models;

public readonly struct DiscoveredLink
{
    public required Uri Uri { get; init; }
    public int? Line { get; init; }
    public string? AnchorText { get; init; }
}