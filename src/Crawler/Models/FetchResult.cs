using System.Net;
using System.Text;

namespace Crawler.Models;

public record FetchResult : IDisposable
{
    public byte[]? Content { private get; init; }
    
    public required bool Success { get; init; }
    public required HttpStatusCode StatusCode { get; init; }
    public string? ContentType { get; init; }

    public string? ReadAsString()
    {
        return Content != null ? Encoding.UTF8.GetString(Content) : null;
    }

    public byte[]? ReadAsByteArray()
    {
        return Content;
    }
    
    public void Dispose() { }
}