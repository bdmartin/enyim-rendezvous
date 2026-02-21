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
public class KeyPatternBenchmarks
{
    private const int KeyCount = 1000;
    private const int NodeCount = 10;

    private RendezvousNodeLocator _locator = null!;
    private string[] _sequentialKeys = null!;
    private string[] _guidKeys = null!;
    private string[] _shortKeys = null!;
    private string[] _longKeys = null!;
    private string[] _realisticKeys = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _locator = new RendezvousNodeLocator(new FnvRendezvousHash());

        var nodes = new List<IMemcachedNode>(NodeCount);
        for (int i = 0; i < NodeCount; i++)
        {
            nodes.Add(CreateMockNode($"10.0.0.{i}", 11211));
        }

        _locator.Initialize(nodes);

        _sequentialKeys = new string[KeyCount];
        _guidKeys = new string[KeyCount];
        _shortKeys = new string[KeyCount];
        _longKeys = new string[KeyCount];
        _realisticKeys = new string[KeyCount];

        for (int i = 0; i < KeyCount; i++)
        {
            _sequentialKeys[i] = $"key-{i}";
            _guidKeys[i] = Guid.NewGuid().ToString();
            _shortKeys[i] = $"{(char)('a' + (i / 26))}{(char)('a' + (i % 26))}";
            _longKeys[i] = new string((char)('a' + (i % 26)), 256);
            _realisticKeys[i] = $"user:{Guid.NewGuid()}:session";
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _locator?.Dispose();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = KeyCount)]
    public void SequentialKeys()
    {
        for (int i = 0; i < KeyCount; i++)
            _locator.Locate(_sequentialKeys[i]);
    }

    [Benchmark(OperationsPerInvoke = KeyCount)]
    public void GuidKeys()
    {
        for (int i = 0; i < KeyCount; i++)
            _locator.Locate(_guidKeys[i]);
    }

    [Benchmark(OperationsPerInvoke = KeyCount)]
    public void ShortKeys()
    {
        for (int i = 0; i < KeyCount; i++)
            _locator.Locate(_shortKeys[i]);
    }

    [Benchmark(OperationsPerInvoke = KeyCount)]
    public void LongKeys()
    {
        for (int i = 0; i < KeyCount; i++)
            _locator.Locate(_longKeys[i]);
    }

    [Benchmark(OperationsPerInvoke = KeyCount)]
    public void RealisticKeys()
    {
        for (int i = 0; i < KeyCount; i++)
            _locator.Locate(_realisticKeys[i]);
    }

    private static IMemcachedNode CreateMockNode(string address, int port)
    {
        var mock = new Mock<IMemcachedNode>();
        mock.Setup(n => n.EndPoint).Returns(new IPEndPoint(IPAddress.Parse(address), port));
        mock.Setup(n => n.IsAlive).Returns(true);
        return mock.Object;
    }
}
