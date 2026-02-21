// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System.Numerics;
using System.Text;

namespace Enyim.Caching.Rendezvous.HashProfiler.Analysis;

public sealed class AvalancheResult
{
    public required double MeanBitFlipPercent { get; init; }
    public required double StdDev { get; init; }
    public required int MinBitsFlipped { get; init; }
    public required int MaxBitsFlipped { get; init; }
    public required bool Passed { get; init; }
}

public static class AvalancheAnalyzer
{
    private const string FixedNode = "10.0.0.1:11211";
    private const int MinKeyLength = 8;
    private const int MaxKeyLength = 16;

    public static AvalancheResult Analyze(IRendezvousHash hash, int iterations)
    {
        var rng = new Random(42);
        var distances = new int[iterations];

        for (int i = 0; i < iterations; i++)
        {
            // Generate random key
            int keyLen = rng.Next(MinKeyLength, MaxKeyLength + 1);
            var keyBytes = new byte[keyLen];
            for (int j = 0; j < keyLen; j++)
                keyBytes[j] = (byte)rng.Next(32, 127); // printable ASCII

            string originalKey = Encoding.ASCII.GetString(keyBytes);
            uint hash1 = hash.ComputeHash(originalKey, FixedNode);

            // Flip 1 random bit
            int byteIndex = rng.Next(keyLen);
            int bitIndex = rng.Next(8);
            keyBytes[byteIndex] ^= (byte)(1 << bitIndex);

            string flippedKey = Encoding.ASCII.GetString(keyBytes);
            uint hash2 = hash.ComputeHash(flippedKey, FixedNode);

            distances[i] = BitOperations.PopCount(hash1 ^ hash2);
        }

        double mean = distances.Average();
        double meanPercent = mean / 32.0 * 100.0;
        double variance = distances.Select(d => (d - mean) * (d - mean)).Average();
        double stdDev = Math.Sqrt(variance);

        return new AvalancheResult
        {
            MeanBitFlipPercent = meanPercent,
            StdDev = stdDev,
            MinBitsFlipped = distances.Min(),
            MaxBitsFlipped = distances.Max(),
            Passed = meanPercent >= 40.0 && meanPercent <= 60.0
        };
    }
}
