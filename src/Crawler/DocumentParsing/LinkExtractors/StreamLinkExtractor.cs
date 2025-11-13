using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using BrokenLinkChecker.DocumentParsing.ModularLinkExtraction.FastParse;
using BrokenLinkChecker.Models.Links;

namespace BrokenLinkChecker.DocumentParsing.ModularLinkExtraction;

public class StreamLinkExtractor : AbstractLinkExtractor<Link>
{
    private readonly StreamingLinkParser _streamParser;

    public StreamLinkExtractor(HtmlParser parser) : base(parser)
    {
        _streamParser = new StreamingLinkParser();
    }

    public override async Task<IEnumerable<Link>> GetLinksFromStream(Stream contentStream, Link referringUrl)
    {
        if (!Uri.TryCreate(referringUrl.Target, UriKind.Absolute, out var thisUrl))
        {
            return Enumerable.Empty<Link>();
        }

        IEnumerable<string> links = await UltraFastLinkExtractor.ExtractHrefsAsync(contentStream).ConfigureAwait(false);

        return links
            .Where(link => Uri.TryCreate(link, UriKind.Absolute, out var uri) && uri.Host == thisUrl.Host)
            .Where(link => !IsExcluded(link))
            .Select(link => new Link(link));
    }

    protected override IEnumerable<Link> GetLinksFromDocument(IDocument document, Link referringUrl)
    {
        throw new NotImplementedException();
    }
}