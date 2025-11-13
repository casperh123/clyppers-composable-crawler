using System.Net;
using AngleSharp.Html.Parser;
using BrokenLinkChecker.DocumentParsing.ModularLinkExtraction;
using BrokenLinkChecker.Models.Links;

namespace BrokenLinkChecker.DocumentParsing.LinkProcessors;

public class BrokenLinkProcessor : ILinkProcessor<IndexedLink>
{
    private readonly HttpClient _httpClient;
    private readonly AbstractLinkExtractor<IndexedLink> _linkExtractor;
    private Dictionary<string, HttpStatusCode> _visitedResources = new();

    public BrokenLinkProcessor(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _linkExtractor = new IndexedLinkExtractor(new HtmlParser(
                new HtmlParserOptions
                {
                    IsKeepingSourceReferences = true
                }
            )
        );
    }

    public async Task<IEnumerable<IndexedLink>> ProcessLinkAsync(IndexedLink link)
    {
        IEnumerable<IndexedLink> links = [];

        if (!_visitedResources.TryGetValue(link.Target, out HttpStatusCode statusCode))
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync(link.Target, HttpCompletionOption.ResponseHeadersRead);
            
            statusCode = response.StatusCode;
            _visitedResources[link.Target] = statusCode;

            if (response.IsSuccessStatusCode)
            {
                await using Stream responseStream = await response.Content.ReadAsStreamAsync();
                links = await _linkExtractor.GetLinksFromStream(responseStream, link);
            }
        }

        link.StatusCode = statusCode;

        return links;
    }

    public void FlushCache()
    {
        _visitedResources = new();
    }
}