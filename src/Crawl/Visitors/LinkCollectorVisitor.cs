using AngleSharp.Html.Dom;
using Crawl.Core.Interfaces;
using Crawl.LinkDiscoverers;
using Crawl.Models;
using System.Threading.Channels;
using Crawl.Core;

namespace Crawl.Visitors;

public class StreamingLinkCollectorVisitor : ICrawlVisitor
{
    private readonly ILinkDiscoverer _linkDiscoverer;
    private readonly Channel<DiscoveredLink> _channel;

    public IAsyncEnumerable<DiscoveredLink> Links => _channel.Reader.ReadAllAsync();

    public StreamingLinkCollectorVisitor(ILinkDiscoverer? linkDiscoverer = null)
    {
        _linkDiscoverer = linkDiscoverer ?? new HtmlLinkDiscoverer();
        _channel = Channel.CreateUnbounded<DiscoveredLink>();
    }

    public async Task VisitAsync(CrawlResult result, IHtmlDocument? document, CancellationToken cancellationToken = default)
    {
        await foreach (DiscoveredLink link in _linkDiscoverer.DiscoverLinks(result.FetchResult, document, cancellationToken))
        {
            await _channel.Writer.WriteAsync(link, cancellationToken);
        }
    }

    public void Complete()
    {
        _channel.Writer.Complete();
    }
}