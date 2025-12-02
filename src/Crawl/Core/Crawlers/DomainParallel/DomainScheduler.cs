using System.Collections.Concurrent;
using System.Threading.Channels;
using Crawl.Models;

namespace Crawl.Core.Crawlers.DomainParallel
{
    public class DomainScheduler
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<CrawlContext>> _queues = new();
        private readonly ConcurrentDictionary<string, int> _inflight = new();
        private readonly ConcurrentDictionary<string, int> _idleTicks = new();

        private readonly int _maxPerDomain;
        private readonly int _idleThreshold;

        private readonly AsyncAutoResetEvent _wake = new();

        public DomainScheduler(int maxRequestsPerDomain, int idleThreshold = 100)
        {
            _maxPerDomain = maxRequestsPerDomain;
            _idleThreshold = idleThreshold;
        }

        public void StopAccepting()
        {
            _wake.Set();
        }

        public void Clear()
        {
            _queues.Clear();
            _inflight.Clear();
            _idleTicks.Clear();
        }

        public void Enqueue(Uri uri, CrawlContext ctx)
        {
            string host = Normalize(uri.Host);

            var q = _queues.GetOrAdd(host, _ => new ConcurrentQueue<CrawlContext>());
            q.Enqueue(ctx);

            _idleTicks[host] = 0;
            _wake.Set();
        }

        public void DecrementInFlight(string host)
        {
            host = Normalize(host);

            _inflight.AddOrUpdate(host, 0, (_, val) => Math.Max(0, val - 1));
            _wake.Set();
        }

        public async Task RunAsync(ChannelWriter<CrawlContext> writer, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                bool dispatched = false;

                foreach (var kvp in _queues.ToArray())
                {
                    if (token.IsCancellationRequested)
                        break;

                    string domain = kvp.Key;
                    var queue = kvp.Value;

                    int inflight = _inflight.GetOrAdd(domain, 0);

                    if (inflight < _maxPerDomain &&
                        queue.TryDequeue(out var ctx))
                    {
                        _inflight.AddOrUpdate(domain, 1, (_, v) => v + 1);
                        await writer.WriteAsync(ctx, token);

                        dispatched = true;
                        _idleTicks[domain] = 0;
                        continue;
                    }

                    CleanupDomainIfIdle(domain, queue);
                }

                // Full frontier exhaustion condition: no queued items anywhere, and no inflight
                bool anyQueued = _queues.Any(kvp => !kvp.Value.IsEmpty);
                if (!anyQueued && _inflight.Values.All(v => v == 0))
                {
                    return;
                }

                if (!dispatched)
                    await _wake.WaitAsync(token);

            }
        }

        private void CleanupDomainIfIdle(string domain, ConcurrentQueue<CrawlContext> queue)
        {
            if (!queue.IsEmpty) { _idleTicks[domain] = 0; return; }

            int inflight = _inflight.GetOrAdd(domain, 0);
            if (inflight > 0) { _idleTicks[domain] = 0; return; }

            int ticks = _idleTicks.AddOrUpdate(domain, 1, (_, t) => t + 1);
            if (ticks >= _idleThreshold)
            {
                _queues.TryRemove(domain, out _);
                _inflight.TryRemove(domain, out _);
                _idleTicks.TryRemove(domain, out _);
            }
        }

        private static string Normalize(string host)
            => host.ToLowerInvariant();

        private class AsyncAutoResetEvent
        {
            private readonly ConcurrentQueue<TaskCompletionSource<bool>> _waiters = new();
            private int _signaled = 0;

            public void Set()
            {
                if (_waiters.TryDequeue(out var w))
                    w.TrySetResult(true);
                else
                    Interlocked.Exchange(ref _signaled, 1);
            }

            public Task WaitAsync(CancellationToken ct)
            {
                if (Interlocked.Exchange(ref _signaled, 0) == 1)
                    return Task.CompletedTask;

                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _waiters.Enqueue(tcs);

                if (ct != CancellationToken.None)
                    ct.Register(() => tcs.TrySetCanceled(ct));

                return tcs.Task;
            }
        }
    }
}
