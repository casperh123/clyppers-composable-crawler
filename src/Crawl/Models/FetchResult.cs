using System.Net;

namespace Crawl.Models;

public record FetchResult
{
    public required Uri Uri { get; init; }
    public string? Content { get; init; }
    public required bool Success { get; init; }
    public required HttpStatusCode StatusCode { get; init; }
    public string? ContentType { get; init; }
}