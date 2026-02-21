// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using Enyim.Caching.Rendezvous.HashProfiler.Analysis;

namespace Enyim.Caching.Rendezvous.HashProfiler.Reporting;

public sealed class AlgorithmResults
{
    public required DistributionResult Distribution { get; init; }
    public required AvalancheResult Avalanche { get; init; }
    public required MonotonicityResult Monotonicity { get; init; }
    public required CollisionResult Collision { get; init; }

    public bool PassesAll() =>
        Distribution.Passed && Avalanche.Passed && Monotonicity.Passed && Collision.Passed;
}

public sealed class ProfileReport
{
    public Dictionary<string, AlgorithmResults> Results { get; } = new();

    public bool PassesAllThresholds() => Results.Values.All(r => r.PassesAll());
}
