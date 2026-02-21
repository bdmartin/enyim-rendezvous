// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

namespace Enyim.Caching.Rendezvous.HashProfiler.Analysis;

public sealed class CollisionResult
{
    public required int TotalKeys { get; init; }
    public required int UniqueHashes { get; init; }
    public required int Collisions { get; init; }
    public required double ExpectedCollisions { get; init; }
    public required bool Passed { get; init; }
}

public static class CollisionAnalyzer
{
    private const string FixedNode = "10.0.0.1:11211";

    public static CollisionResult Analyze(IRendezvousHash hash, int keyCount)
    {
        var seen = new HashSet<uint>(keyCount);

        for (int i = 0; i < keyCount; i++)
        {
            uint h = hash.ComputeHash($"key-{i}", FixedNode);
            seen.Add(h);
        }

        int uniqueHashes = seen.Count;
        int collisions = keyCount - uniqueHashes;

        // Birthday paradox: expected collisions ~ N^2 / (2 * 2^32)
        double expectedCollisions = (double)keyCount * keyCount / (2.0 * 4294967296.0);

        bool passed = collisions <= Math.Max(5, expectedCollisions * 5);

        return new CollisionResult
        {
            TotalKeys = keyCount,
            UniqueHashes = uniqueHashes,
            Collisions = collisions,
            ExpectedCollisions = expectedCollisions,
            Passed = passed
        };
    }
}
