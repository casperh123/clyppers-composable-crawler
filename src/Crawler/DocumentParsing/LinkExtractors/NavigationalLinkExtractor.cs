using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using BrokenLinkChecker.Models.Links;

namespace BrokenLinkChecker.DocumentParsing.ModularLinkExtraction;

public class NavigationLinkExtractor() : AbstractLinkExtractor<Link>(new HtmlParser(new HtmlParserOptions()))
{
    protected override IEnumerable<Link> GetLinksFromDocument(IDocument document, Link referringUrl)
    {
        List<Link> links = [];
        var thisUrl = new Uri(referringUrl.Target);

        foreach (var link in document.Links)
        {
            var href = link.GetAttribute("href");
            if (!string.IsNullOrEmpty(href) && IsPage(href))
            {
                Link newLink = new(href);
                links.Add(newLink);
            }
        }

        return links
            .Where(link => Uri.TryCreate(link.Target, UriKind.Absolute, out var uri) && uri.Host == thisUrl.Host)
            .ToList();
    }
}