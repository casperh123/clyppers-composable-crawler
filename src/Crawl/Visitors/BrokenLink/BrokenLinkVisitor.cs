using System.Net;
using AngleSharp.Html.Dom;
using Crawl.Core;
using Crawl.Models;

namespace Crawl.Visitors.BrokenLink;

public class BrokenLinkVisitor : ICrawlVisitor
{
    private readonly Dictionary<string, HttpStatusCode> _statusCodes = [];
    private readonly Dictionary<string, List<LinkReference>> _references = [];

    public Task VisitAsync(CrawlResult result, IHtmlDocument? document, CancellationToken cancellationToken = default)
    {
        var currentUrl = result.Context.Uri.AbsoluteUri;

        _statusCodes[currentUrl] = result.FetchResult.StatusCode;

        foreach (var link in result.DiscoveredLinks)
        {
            var key = link.Uri.AbsoluteUri;

            if (!_references.TryGetValue(key, out var list))
            {
                list = [];
                _references[key] = list;
            }

            list.Add(new LinkReference(currentUrl, link.Line, link.AnchorText));
        }

        return Task.CompletedTask;
    }

    public IEnumerable<BrokenLinkReport> GetBrokenLinks()
    {
        foreach (var (url, statusCode) in _statusCodes)
        {
            if ((int)statusCode >= 400)
            {
                yield return new BrokenLinkReport(
                    url,
                    statusCode,
                    _references.GetValueOrDefault(url, [])
                );
            }
        }
    }
}

public readonly record struct LinkReference(string LinkedFrom, int? Line, string? AnchorText);

public readonly record struct BrokenLinkReport(string Url, HttpStatusCode StatusCode, List<LinkReference> References);