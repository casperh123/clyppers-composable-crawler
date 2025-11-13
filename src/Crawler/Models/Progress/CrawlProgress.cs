namespace BrokenLinkChecker.Models.Result;

public record CrawlProgress<T>
{
    public CrawlProgress(T link, int linksChecked, int linksEnqueued)
    {
        Link = link;
        LinksChecked = linksChecked;
        LinksEnqueued = linksEnqueued;
    }

    public T Link { get; set; }
    public int LinksChecked { get; init; }
    public int LinksEnqueued { get; init; }
}