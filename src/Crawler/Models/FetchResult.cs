namespace Crawler.Models;

public record FetchResult : IDisposable
{
    public bool IsSuccess { get; set; }
    
    public void Dispose() { }
}