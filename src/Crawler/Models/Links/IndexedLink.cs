using System.Net;

namespace BrokenLinkChecker.Models.Links;

public class IndexedLink : Link
{
    public IndexedLink(TraceableLink url, HttpStatusCode statuscode) : base(url.Target)
    {
        ReferringPage = url.Referrer;
        AnchorText = url.AnchorText;
        Line = url.Line;
        StatusCode = statuscode;
    }

    public IndexedLink(string referrer, string target, string anchorText, int line,
        HttpStatusCode statuscode = HttpStatusCode.Unused) : base(target)
    {
        ReferringPage = referrer;
        AnchorText = anchorText;
        Line = line;
        StatusCode = statuscode;
    }

    public IndexedLink(string target) : base(target) {}

    public string ReferringPage { get; set; }
    public string AnchorText { get; set; }
    public int Line { get; set; }
    public HttpStatusCode StatusCode { get; set; }
}