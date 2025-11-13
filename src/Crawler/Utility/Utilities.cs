using System.Diagnostics;

namespace BrokenLinkChecker.Utility;

public static class Utilities
{
    public static string GetUrl(string baseUrl, string href)
    {
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) &&
            Uri.TryCreate(baseUri, href, out var resultUri))
            return resultUri.ToString();
        return href;
    }

    public static async Task<(T, long)> BenchmarkAsync<T>(Func<Task<T>> function)
    {
        var stopwatch = Stopwatch.StartNew();

        var result = await function.Invoke();

        return (result, stopwatch.ElapsedMilliseconds);
    }
}