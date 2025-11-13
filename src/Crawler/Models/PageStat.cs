using System.Net;
using BrokenLinkChecker.Models.Headers;

namespace BrokenLinkChecker.Models;

public record PageStat
{
    public PageStat(string url, HttpResponseMessage response, ResourceType type, long responseTime = 0,
        long parseTime = 0)
    {
        Url = url;
        ResponseTime = responseTime;
        DocumentParseTime = parseTime;
        Size = response.Content.Headers.ContentLength ?? 0;
        StatusCode = response.StatusCode;
        HttpVersion = response.Version.ToString();
        Headers = new PageHeaders(response.Headers, response.Content.Headers);
        Type = type;
    }

    public string Url { get; init; }
    public HttpStatusCode StatusCode { get; private set; }
    public long ResponseTime { get; }
    public long DocumentParseTime { get; }
    public long CombinedTime => ResponseTime + DocumentParseTime;
    public long Size { get; }
    public float SizeKb => Size / 1024f;
    public string HttpVersion { get; private set; }
    public PageHeaders Headers { get; private set; }
    public ResourceType Type { get; private set; }
}