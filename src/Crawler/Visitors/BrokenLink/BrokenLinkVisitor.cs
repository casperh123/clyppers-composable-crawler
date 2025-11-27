using System.Net;
using AngleSharp.Html.Dom;
using Crawler.Core;
using Crawler.Models;

namespace Crawler.Visitors.BrokenLink;

public class BrokenLinkVisitor : ICrawlVisitor
{
    private readonly Dictionary<string, HttpStatusCode> _statusCodes = [];
    private readonly Dictionary<string, IList<(Uri Referrer, DiscoveredLink Link)>> _references = [];
    
    public Task VisitAsync(CrawlResult result, IHtmlDocument? document, CancellationToken cancellationToken = default)
    {
        _statusCodes[result.Context.Uri.AbsoluteUri] = result.FetchResult.StatusCode;

        foreach (DiscoveredLink link in result.DiscoveredLinks)
        {
            string key = link.Uri.AbsoluteUri;

            if (!_references.ContainsKey(key))
            {
                _references[key] = [];
            }
            
            _references[key].Add((result.Context.Uri, link));
        }

        return Task.CompletedTask;
    }
    
    public IEnumerable<BrokenLinkReport> GetBrokenLinks()
    {
        foreach (var (url, statusCode) in _statusCodes)
        {
            if ((int)statusCode >= 400)
            {
                yield return new BrokenLinkReport
                {
                    Uri = new Uri(url),
                    StatusCode = statusCode,
                    References = _references.GetValueOrDefault(url, [])
                };
            }
        }
    }
}

public record BrokenLinkReport
{
    public required Uri Uri { get; init; }
    public required HttpStatusCode StatusCode { get; init; }
    public required IList<(Uri Referrer, DiscoveredLink Link)> References { get; init; }
}