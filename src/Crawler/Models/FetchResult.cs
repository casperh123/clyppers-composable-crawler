using System.Net;
using System.Text;

namespace Crawler.Models;

public record FetchResult
{
    public string? Content { get; init; }
    public required bool Success { get; init; }
    public required HttpStatusCode StatusCode { get; init; }
    public string? ContentType { get; init; }
}