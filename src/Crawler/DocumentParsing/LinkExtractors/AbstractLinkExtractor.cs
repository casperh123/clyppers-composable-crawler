using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using BrokenLinkChecker.Models.Links;

namespace BrokenLinkChecker.DocumentParsing.ModularLinkExtraction;

public abstract class AbstractLinkExtractor<T> where T : Link
{
    protected readonly HtmlParser Parser;

    public AbstractLinkExtractor(HtmlParser parser)
    {
        Parser = parser;
    }

    public virtual async Task<IEnumerable<T>> GetLinksFromStream(Stream contentStream, T referringUrl)
    {
        return GetLinksFromDocument(
            await Parser.ParseDocumentAsync(contentStream).ConfigureAwait(false),
            referringUrl
        );
    }

    protected abstract IEnumerable<T> GetLinksFromDocument(IDocument document, T referringUrl);

    protected static bool IsPage(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        if (IsResourceFile(uri)) return false;

        return !IsExcluded(url);
    }

    private static readonly HashSet<string> ExcludedExtensions = new(
        new[] { ".js", ".css", ".png", ".jpg", ".jpeg", ".pdf", ".svg" },
        StringComparer.OrdinalIgnoreCase
    );

    protected static bool IsResourceFile(Uri uri)
    {
        return ExcludedExtensions.Contains(Path.GetExtension(uri.LocalPath));
    }

    protected static bool IsExcluded(string url)
    {
        return url.Contains('#') || url.Contains('?') || url.Contains("mailto:");
    }
}