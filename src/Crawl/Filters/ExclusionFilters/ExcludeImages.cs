using Crawl.Core.Interfaces;
using Crawl.Models;

namespace Crawl.Filters.ExclusionFilters;

public class ExcludeImages : ICrawlFilter
{
    private static readonly HashSet<string> _exclude =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".bmp",
            ".tiff",
            ".tif",
            ".webp",
            ".svg",
            ".ico",
            ".heic",
            ".heif"
        };
    
    public bool ShouldCrawl(CrawlContext context)
    {
        string extension = Path.GetExtension(context.Uri.LocalPath);

        if (string.IsNullOrEmpty(extension))
            return true;

        return !_exclude.Contains(extension);
    }

}