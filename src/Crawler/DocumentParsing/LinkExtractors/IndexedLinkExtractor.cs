using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using BrokenLinkChecker.Models.Links;
using BrokenLinkChecker.Utility;

namespace BrokenLinkChecker.DocumentParsing.ModularLinkExtraction;

public class IndexedLinkExtractor(HtmlParser parser) : AbstractLinkExtractor<IndexedLink>(parser)
{
    protected override IEnumerable<IndexedLink> GetLinksFromDocument(IDocument document, IndexedLink referringUrl)
    {
        List<IndexedLink> links = [];
        Uri thisUrl = new Uri(referringUrl.Target);

        foreach (var link in document.Links)
        {
            var href = link.GetAttribute("href") ?? string.Empty;
            links.Add(GenerateIndexedLink(link, referringUrl.Target, href));
        }

        return links
            .Where(link => Uri.TryCreate(link.Target, UriKind.Absolute, out var uri) && uri.Host == thisUrl.Host)
            .Where(link => !IsExcluded(link.Target))
            .ToList();
    }

    private IndexedLink GenerateIndexedLink(IElement element, string referringPage, string target)
    {
        var href = target;
        var resolvedUrl = Utilities.GetUrl(target, href);
        var text = element.TextContent;
        var line = element.SourceReference?.Position.Line ?? -1;

        return new IndexedLink(referringPage, resolvedUrl, text, line);
    }
}