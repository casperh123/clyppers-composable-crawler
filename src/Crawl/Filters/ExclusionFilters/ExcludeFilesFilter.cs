using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.Filters.ExclusionFilters;

public class ExcludeFilesFilter : ICrawlFilter
{
    private readonly ISet<string> _exclude = new HashSet<string>([".pdf", ".zip"]);
    
    public bool ShouldCrawl(CrawlContext context)
    {
        string extension = Path.GetExtension(context.Uri.LocalPath);

        if (string.IsNullOrEmpty(extension))
            return true;

        return !_exclude.Contains(extension);
    }

}