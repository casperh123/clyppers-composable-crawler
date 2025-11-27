using System.Net;

namespace Crawler.Models;

public struct FetchResult
{
    public Uri Uri { get; set; }
    public string? Content { get; init; }
    public required bool Success { get; init; }
    public required HttpStatusCode StatusCode { get; init; }
    public string? ContentType { get; init; }
}