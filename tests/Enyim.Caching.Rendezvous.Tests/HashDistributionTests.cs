// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Enyim.Caching.Memcached;
using Enyim.Caching.Rendezvous.Hashing;
using Moq;
using Xunit;

namespace Enyim.Caching.Rendezvous.Tests
{
    public class HashDistributionTests
    {
        private static IMemcachedNode CreateMockNode(string address, int port)
        {
            var mock = new Mock<IMemcachedNode>();
            mock.Setup(n => n.EndPoint).Returns(new IPEndPoint(IPAddress.Parse(address), port));
            mock.Setup(n => n.IsAlive).Returns(true);
            return mock.Object;
        }

        private static Dictionary<IMemcachedNode, int> MeasureDistribution(
            IRendezvousHash hash, int nodeCount, int keyCount)
        {
            var nodes = new List<IMemcachedNode>();
            for (int i = 1; i <= nodeCount; i++)
                nodes.Add(CreateMockNode($"10.0.0.{i}", 11211));

            var locator = new RendezvousNodeLocator(hash);
            locator.Initialize(nodes);

            var distribution = new Dictionary<IMemcachedNode, int>();
            foreach (var node in nodes)
                distribution[node] = 0;

            for (int i = 0; i < keyCount; i++)
                distribution[locator.Locate($"key-{i}")]++;

            return distribution;
        }

        [Theory]
        [InlineData(typeof(FnvRendezvousHash))]
        [InlineData(typeof(MurmurHash3RendezvousHash))]
        [InlineData(typeof(Sha256RendezvousHash))]
        public void Distribution_IsReasonablyUniform(Type hashType)
        {
            var hash = (IRendezvousHash)Activator.CreateInstance(hashType);
            int nodeCount = 5;
            int keyCount = 10000;
            double expectedPerNode = (double)keyCount / nodeCount; // 2000

            var distribution = MeasureDistribution(hash, nodeCount, keyCount);

            foreach (var kvp in distribution)
            {
                double deviation = Math.Abs(kvp.Value - expectedPerNode) / expectedPerNode * 100;
                // Each node should be within 20% of the ideal even split
                Assert.True(deviation < 20,
                    $"Node {kvp.Key.EndPoint} received {kvp.Value} keys ({deviation:F1}% deviation from expected {expectedPerNode}). Hash: {hashType.Name}");
            }
        }

        [Theory]
        [InlineData(typeof(FnvRendezvousHash))]
        [InlineData(typeof(MurmurHash3RendezvousHash))]
        [InlineData(typeof(Sha256RendezvousHash))]
        public void AllHashAlgorithms_AreDeterministic(Type hashType)
        {
            var hash = (IRendezvousHash)Activator.CreateInstance(hashType);

            for (int i = 0; i < 100; i++)
            {
                var key = $"key-{i}";
                var node = $"10.0.0.{(i % 5) + 1}:11211";
                uint first = hash.ComputeHash(key, node);
                uint second = hash.ComputeHash(key, node);
                Assert.Equal(first, second);
            }
        }

        [Theory]
        [InlineData(typeof(FnvRendezvousHash))]
        [InlineData(typeof(MurmurHash3RendezvousHash))]
        [InlineData(typeof(Sha256RendezvousHash))]
        public void DifferentKeyNodePairs_ProduceDifferentHashes(Type hashType)
        {
            var hash = (IRendezvousHash)Activator.CreateInstance(hashType);
            var hashes = new HashSet<uint>();

            // Generate 1000 different key-node pairs
            for (int i = 0; i < 1000; i++)
            {
                hashes.Add(hash.ComputeHash($"key-{i}", "10.0.0.1:11211"));
            }

            // With a 32-bit hash and 1000 values, collisions are unlikely.
            // Expect at least 990 unique values.
            Assert.True(hashes.Count > 990,
                $"Expected >990 unique hashes but got {hashes.Count}. Hash: {hashType.Name}");
        }
    }
}
