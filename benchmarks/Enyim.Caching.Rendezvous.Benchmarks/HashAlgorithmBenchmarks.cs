// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Enyim.Caching.Rendezvous;
using Enyim.Caching.Rendezvous.Hashing;

namespace Enyim.Caching.Rendezvous.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class HashAlgorithmBenchmarks
{
    private const string Node = "10.0.0.1:11211";
    private const string ShortKey = "k1";
    private const string MediumKey = "user:session:abc123def456";
    private static readonly string LongKey = new string('x', 256);

    private readonly FnvRendezvousHash _fnv = new FnvRendezvousHash();
    private readonly MurmurHash3RendezvousHash _murmur = new MurmurHash3RendezvousHash();
    private readonly Sha256RendezvousHash _sha256 = new Sha256RendezvousHash();

    [Benchmark(Baseline = true)]
    public uint Fnv_ShortKey() => _fnv.ComputeHash(ShortKey, Node);

    [Benchmark]
    public uint Fnv_MediumKey() => _fnv.ComputeHash(MediumKey, Node);

    [Benchmark]
    public uint Fnv_LongKey() => _fnv.ComputeHash(LongKey, Node);

    [Benchmark]
    public uint Murmur_ShortKey() => _murmur.ComputeHash(ShortKey, Node);

    [Benchmark]
    public uint Murmur_MediumKey() => _murmur.ComputeHash(MediumKey, Node);

    [Benchmark]
    public uint Murmur_LongKey() => _murmur.ComputeHash(LongKey, Node);

    [Benchmark]
    public uint Sha256_ShortKey() => _sha256.ComputeHash(ShortKey, Node);

    [Benchmark]
    public uint Sha256_MediumKey() => _sha256.ComputeHash(MediumKey, Node);

    [Benchmark]
    public uint Sha256_LongKey() => _sha256.ComputeHash(LongKey, Node);
}
