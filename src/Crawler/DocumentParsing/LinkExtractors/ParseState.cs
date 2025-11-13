namespace BrokenLinkChecker.DocumentParsing.ModularLinkExtraction;

public enum ParseState
{
    InHrefValue,
    SearchingForHref,
    SearchingForQuote,
}