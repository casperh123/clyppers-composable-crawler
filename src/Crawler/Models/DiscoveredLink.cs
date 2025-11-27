namespace Crawler.Models;

public record DiscoveredLink
{
    public required Uri Uri { get; set; }
    public int? Line { get; set; }
    public string? AnchorText { get; set; }
}