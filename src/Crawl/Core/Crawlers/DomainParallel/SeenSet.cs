using Standart.Hash.xxHash;

namespace Crawl.Core.Crawlers.DomainParallel;

public class SeenSet
{
    private readonly ulong[] _buckets;
    private readonly int _hashFunctions;
    private readonly ulong _mask;

    public SeenSet(int expectedItems, double falsePositiveRate = 0.01)
    {
        double ln2 = Math.Log(2);
        double bitsPerItem = -Math.Log(falsePositiveRate) / (ln2 * ln2);
        ulong totalBits = (ulong)(expectedItems * bitsPerItem);
        ulong bucketCount = (totalBits + 63UL) / 64UL;

        _buckets = new ulong[bucketCount];
        _hashFunctions = Math.Max(1, (int)Math.Round(bitsPerItem * ln2));
        _mask = bucketCount * 64UL - 1;
    }

    public bool TryAdd(string url)
    {
        ulong h1 = xxHash64.ComputeHash(url);
        ulong h2 = (h1 >> 32) | 1UL; // ensure h2 is odd
        bool allSet = true;

        for (int i = 0; i < _hashFunctions; i++)
        {
            ulong bitIndex = (h1 + (ulong)i * h2) & _mask;
            int bucket = (int)(bitIndex >> 6);
            ulong bit = 1UL << (int)(bitIndex & 63UL);

            ulong oldValue, newValue;
            do
            {
                oldValue = Volatile.Read(ref _buckets[bucket]);
                if ((oldValue & bit) != 0)
                    break; // already set
                newValue = oldValue | bit;
            } while (Interlocked.CompareExchange(ref _buckets[bucket], newValue, oldValue) != oldValue);

            if ((oldValue & bit) == 0)
                allSet = false;
        }

        return !allSet;
    }

    public void Clear()
    {
        Array.Clear(_buckets, 0, _buckets.Length);
    }
}