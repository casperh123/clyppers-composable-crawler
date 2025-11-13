using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using BrokenLinkChecker.Models.Links;

namespace BrokenLinkChecker.DocumentParsing.ModularLinkExtraction;

public class LinkExtractor : AbstractLinkExtractor<Link>
{
    // Cache the delegate for Uri.TryCreate to avoid repeated delegate creation
    private static readonly TryParseDelegate<Uri?> TryCreateUri = Uri.TryCreate;

    private delegate bool TryParseDelegate<T>(string uriString, UriKind uriKind, out T result);

    private const int InitialLinksCapacity = 256;

    public LinkExtractor(HtmlParser parser) : base(parser)
    {
    }

    protected override IEnumerable<Link> GetLinksFromDocument(IDocument document, Link referringUrl)
    {
        string currentHost = new Uri(referringUrl.Target).Host;
        List<Link> links = new List<Link>(InitialLinksCapacity);

        ReadOnlySpan<char> hrefSpan;
        ReadOnlySpan<char> srcSpan;

        IHtmlCollection<IElement> elements = document.QuerySelectorAll("a[href], img[src]");

        foreach (IElement element in elements)
        {
            string? attributeValue;

            if (element is IHtmlAnchorElement anchor)
            {
                attributeValue = anchor.Href;
                hrefSpan = attributeValue.AsSpan();

                if (!hrefSpan.IsEmpty && !IsExcludedFast(hrefSpan) &&
                    IsValidHostMatch(attributeValue, currentHost))
                {
                    links.Add(new Link(attributeValue));
                }
            }
            else if (element is IHtmlImageElement img)
            {
                attributeValue = img.Source;
                srcSpan = attributeValue.AsSpan();

                if (!srcSpan.IsEmpty && !IsExcludedFast(srcSpan) &&
                    IsValidHostMatch(attributeValue, currentHost))
                {
                    links.Add(new Link(attributeValue));
                }
            }
        }

        return links;
    }

    private static bool IsExcludedFast(ReadOnlySpan<char> url)
    {
        if (url.Contains('#') || url.Contains('?'))
        {
            return true;
        }

        if (url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    // Separate method for host matching to avoid repeated Uri creation
    private static bool IsValidHostMatch(string url, string currentHost)
    {
        if (TryCreateUri(url, UriKind.Absolute, out var uri))
        {
            return string.Equals(uri.Host, currentHost, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}