using System.Collections.Concurrent;
using System.Threading.Channels;
using Crawl.Models;

namespace Crawl.Core.Crawlers.DomainParallel
{
    public class DomainScheduler
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<CrawlContext>> _domainQueues;
        private readonly ConcurrentDictionary<string, int> _inFlightRequests;
        private readonly ConcurrentDictionary<string, string> _hostCache;
        private readonly int _maxRequestsPerDomain;

        private int _domainIndex;

        private readonly object _wakeLock = new object();
        private TaskCompletionSource<bool>? _wakeSignal;

        public DomainScheduler(int maxRequestsPerDomain)
        {
            _maxRequestsPerDomain = maxRequestsPerDomain;
            _domainQueues = new ConcurrentDictionary<string, ConcurrentQueue<CrawlContext>>();
            _inFlightRequests = new ConcurrentDictionary<string, int>();
            _hostCache = new ConcurrentDictionary<string, string>();
            _domainIndex = 0;
        }

        public void Clear()
        {
            _domainQueues.Clear();
            _inFlightRequests.Clear();
            _hostCache.Clear();
            _domainIndex = 0;
        }

        public void Enqueue(Uri uri, CrawlContext context)
        {
            string host = GetCachedHost(uri.Host);
            ConcurrentQueue<CrawlContext> queue = _domainQueues.GetOrAdd(
                host,
                _ => new ConcurrentQueue<CrawlContext>());

            queue.Enqueue(context);
            SignalWake();
        }

        public void DecrementInFlight(string host)
        {
            string normalizedHost = GetCachedHost(host);
            _inFlightRequests.AddOrUpdate(
                normalizedHost,
                0,
                (_, current) => Math.Max(0, current - 1));

            SignalWake();
        }

        public async Task RunScheduler(ChannelWriter<CrawlContext> writer, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_domainQueues.IsEmpty)
                {
                    await WaitForWakeAsync(cancellationToken);
                    continue;
                }

                bool dispatchedAny = false;
                int currentIndex = 0;
                int domainCount = _domainQueues.Count;

                foreach (KeyValuePair<string, ConcurrentQueue<CrawlContext>> pair in _domainQueues)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Simple round-robin: skip until the domain index matches
                    if (currentIndex < _domainIndex)
                    {
                        currentIndex++;
                        continue;
                    }

                    string domain = pair.Key;
                    bool dispatched = await TryDispatchDomainAsync(domain, writer, cancellationToken);

                    if (dispatched)
                    {
                        dispatchedAny = true;
                    }

                    currentIndex++;
                    if (currentIndex >= domainCount)
                    {
                        _domainIndex = 0;
                        break;
                    }
                }

                _domainIndex++;
                if (_domainIndex >= domainCount)
                {
                    _domainIndex = 0;
                }

                if (!dispatchedAny)
                {
                    await WaitForWakeAsync(cancellationToken);
                }
            }

            writer.TryComplete();
        }

        private async ValueTask<bool> TryDispatchDomainAsync(
            string domain,
            ChannelWriter<CrawlContext> writer,
            CancellationToken cancellationToken)
        {
            if (!_domainQueues.TryGetValue(domain, out ConcurrentQueue<CrawlContext>? queue))
            {
                return false;
            }

            if (_inFlightRequests.TryGetValue(domain, out int inFlightCount) &&
                inFlightCount >= _maxRequestsPerDomain)
            {
                return false;
            }

            if (!queue.TryDequeue(out CrawlContext context))
            {
                _domainQueues.TryRemove(domain, out _);
                return false;
            }

            _inFlightRequests.AddOrUpdate(domain, 1, (_, current) => current + 1);
            await writer.WriteAsync(context, cancellationToken);
            return true;
        }

        private string GetCachedHost(string host)
        {
            // Avoid creating multiple lowercased strings for the same host
            string normalized = host.ToLowerInvariant();
            return _hostCache.GetOrAdd(normalized, normalized);
        }

        private void SignalWake()
        {
            lock (_wakeLock)
            {
                if (_wakeSignal != null)
                {
                    _wakeSignal.TrySetResult(true);
                    _wakeSignal = null;
                }
            }
        }

        private async Task WaitForWakeAsync(CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool> localSignal;

            lock (_wakeLock)
            {
                if (_wakeSignal == null)
                {
                    _wakeSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                }

                localSignal = _wakeSignal;
            }

            await using (cancellationToken.Register(() => localSignal.TrySetCanceled()))
            {
                await localSignal.Task;
            }
        }
    }
}
