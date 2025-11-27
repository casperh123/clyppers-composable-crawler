namespace Crawler.Utils;

public static class UriNormalizer
{
    public static Uri Normalize(Uri uri)
    {
        var host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? uri.Host[4..]
            : uri.Host;

        return new UriBuilder(uri)
        {
            Host = host.ToLowerInvariant(),
            Path = uri.AbsolutePath.ToLowerInvariant(),
            Fragment = string.Empty
        }.Uri;
    }
}