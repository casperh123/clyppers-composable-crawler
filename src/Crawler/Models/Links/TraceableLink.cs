namespace BrokenLinkChecker.Models.Links;

public class TraceableLink(string referrer, string target, string anchorText, int line, ResourceType type)
    : Link(target)
{
    public string Referrer = referrer;
    public string AnchorText { get; } = anchorText;
    public int Line { get; } = line;
    public ResourceType Type { get; } = type;
}