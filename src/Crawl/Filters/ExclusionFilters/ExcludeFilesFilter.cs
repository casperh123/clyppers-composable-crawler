using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.Filters;

public class ExcludeFilesFilter : ICrawlFilter
{
    private readonly ISet<string> _excluded = new HashSet<string>([".pdf", ".zip"]);
    
    public bool ShouldCrawl(CrawlContext context)
    {
        string extension = Path.GetExtension(context.Uri.LocalPath);

        if (string.IsNullOrEmpty(extension))
            return true;

        return !_excluded.Contains(extension);
    }

}