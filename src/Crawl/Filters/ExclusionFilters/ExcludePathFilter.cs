using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.Filters.ExclusionFilters;

public class ExcludePathFilter : ICrawlFilter
{
    private readonly string[] _exclude;

    public ExcludePathFilter(params string[] excludePaths)
    {
        _exclude = excludePaths;
    }

    public bool ShouldCrawl(CrawlContext context)
    {
        foreach (string exludedPath in _exclude)
        {
            if(context.Uri.AbsolutePath.ContainsAny(exludedPath.AsSpan()))
            {
                return false;
            }
        }

        return true;
    }
}