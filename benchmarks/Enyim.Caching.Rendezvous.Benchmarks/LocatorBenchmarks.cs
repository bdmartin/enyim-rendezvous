// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System.Net;
using BenchmarkDotNet.Attributes;
using Enyim.Caching.Memcached;
using Enyim.Caching.Rendezvous;
using Enyim.Caching.Rendezvous.Hashing;
using Moq;

namespace Enyim.Caching.Rendezvous.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class LocatorBenchmarks
{
    [Params(1, 5, 10, 50, 100)]
    public int NodeCount { get; set; }

    [ParamsSource(nameof(HashNames))]
    public string HashName { get; set; } = null!;

    public static IEnumerable<string> HashNames => new[] { "FNV", "Murmur", "SHA256" };

    private RendezvousNodeLocator _locator = null!;
    private string[] _keys = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        IRendezvousHash hash = HashName switch
        {
            "FNV" => new FnvRendezvousHash(),
            "Murmur" => new MurmurHash3RendezvousHash(),
            "SHA256" => new Sha256RendezvousHash(),
            _ => throw new ArgumentException($"Unknown hash: {HashName}")
        };

        _locator = new RendezvousNodeLocator(hash);

        var nodes = new List<IMemcachedNode>(NodeCount);
        for (int i = 0; i < NodeCount; i++)
        {
            nodes.Add(CreateMockNode($"10.0.{i / 256}.{i % 256}", 11211));
        }

        _locator.Initialize(nodes);

        _keys = new string[1000];
        for (int i = 0; i < 1000; i++)
        {
            _keys[i] = $"key-{i}";
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _locator?.Dispose();
    }

    [Benchmark]
    public IMemcachedNode LocateSingleKey() => _locator.Locate(_keys[0]);

    [Benchmark(OperationsPerInvoke = 1000)]
    public void Locate1000Keys()
    {
        for (int i = 0; i < 1000; i++)
        {
            _locator.Locate(_keys[i]);
        }
    }

    private static IMemcachedNode CreateMockNode(string address, int port)
    {
        var mock = new Mock<IMemcachedNode>();
        mock.Setup(n => n.EndPoint).Returns(new IPEndPoint(IPAddress.Parse(address), port));
        mock.Setup(n => n.IsAlive).Returns(true);
        return mock.Object;
    }
}
