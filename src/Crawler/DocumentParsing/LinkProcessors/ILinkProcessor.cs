using BrokenLinkChecker.Models.Links;

namespace BrokenLinkChecker.DocumentParsing.LinkProcessors;

public interface ILinkProcessor<T> where T : Link
{
    public Task<IEnumerable<T>> ProcessLinkAsync(T link);
    public void FlushCache();
}