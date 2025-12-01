using System.Diagnostics;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Crawl.Models;

namespace Crawl.Core;

public class SequentialCrawler : Crawler
{
    public SequentialCrawler(
        ICrawlFilter filter, 
        IFetcher fetcher, 
        ILinkDiscoverer discoverer, 
        ICrawlVisitor visitor
        ) : base(filter, fetcher, discoverer, visitor)
    {
    }

    public override async Task CrawlAsync(Uri startUri, IProgress<CrawlProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        HashSet<string> seen = [];
        Queue<CrawlContext> queue = new();
        int totalCrawled = 0;

        seen.Add(startUri.AbsoluteUri);
        queue.Enqueue(new CrawlContext
        {
            Uri = startUri,
            ReferringUri = null,
            Depth = 0
        });

        progress?.Report(CrawlProgress.Started());

        while (queue.TryDequeue(out CrawlContext? context) && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!Filter.ShouldCrawl(context))
                {
                    continue;
                }

                IEnumerable<CrawlContext> foundLinks = await ProcessUriAsync(context);

                foreach (CrawlContext foundLink in foundLinks)
                {
                    if (seen.Add(foundLink.Uri.AbsoluteUri))
                    {
                        queue.Enqueue(foundLink);
                    }
                }

                totalCrawled += 1;

                progress?.Report(CrawlProgress.Progress(context, totalCrawled, queue.Count));
            }
            catch (Exception ex)
            {
                progress?.Report(CrawlProgress.Error(context, ex, totalCrawled, queue.Count));
            }
        }
    }
}