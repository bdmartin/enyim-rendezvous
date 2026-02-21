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

namespace Enyim.Caching.Rendezvous.IntegrationTests.EndToEnd
{
    public class FactoryToLocatorFlowTests
    {
        private static IMemcachedNode CreateMockNode(string address, int port, bool isAlive = true)
        {
            var mock = new Mock<IMemcachedNode>();
            mock.Setup(n => n.EndPoint).Returns(new IPEndPoint(IPAddress.Parse(address), port));
            mock.Setup(n => n.IsAlive).Returns(isAlive);
            return mock.Object;
        }

        [Fact]
        public void FullFlow_DefaultFactory_LocatesKeysCorrectly()
        {
            var factory = new RendezvousNodeLocatorFactory();
            var locator = factory.Create();

            var nodes = new List<IMemcachedNode>();
            for (int i = 1; i <= 5; i++)
                nodes.Add(CreateMockNode($"10.0.0.{i}", 11211));

            locator.Initialize(nodes);

            // Locate 100 keys — each call should be deterministic
            var mappings = new Dictionary<string, IMemcachedNode>();
            for (int i = 0; i < 100; i++)
            {
                var key = $"key-{i}";
                mappings[key] = locator.Locate(key);
            }

            // Verify determinism: same key always returns same node
            for (int i = 0; i < 100; i++)
            {
                var key = $"key-{i}";
                Assert.Same(mappings[key], locator.Locate(key));
            }

            // Verify all 5 nodes are used across the 100 keys
            var usedNodes = new HashSet<IMemcachedNode>(mappings.Values);
            Assert.Equal(5, usedNodes.Count);
        }

        [Theory]
        [InlineData(typeof(FnvRendezvousHash))]
        [InlineData(typeof(MurmurHash3RendezvousHash))]
        [InlineData(typeof(Sha256RendezvousHash))]
        public void FullFlow_EachHashAlgorithm(Type hashType)
        {
            var hash = (IRendezvousHash)Activator.CreateInstance(hashType);
            var factory = new RendezvousNodeLocatorFactory(hash);
            var locator = factory.Create();

            var nodes = new List<IMemcachedNode>();
            for (int i = 1; i <= 5; i++)
                nodes.Add(CreateMockNode($"10.0.0.{i}", 11211));

            locator.Initialize(nodes);

            var mappings = new Dictionary<string, IMemcachedNode>();
            for (int i = 0; i < 1000; i++)
            {
                var key = $"key-{i}";
                mappings[key] = locator.Locate(key);
            }

            // Determinism
            for (int i = 0; i < 1000; i++)
            {
                var key = $"key-{i}";
                Assert.Same(mappings[key], locator.Locate(key));
            }

            // All nodes used
            var usedNodes = new HashSet<IMemcachedNode>(mappings.Values);
            Assert.Equal(5, usedNodes.Count);
        }

        [Fact]
        public void FullFlow_MultipleLocators_IndependentState()
        {
            var factory = new RendezvousNodeLocatorFactory();
            var locator1 = factory.Create();
            var locator2 = factory.Create();

            var nodesA = new List<IMemcachedNode>
            {
                CreateMockNode("10.0.0.1", 11211),
                CreateMockNode("10.0.0.2", 11211),
            };

            var nodesB = new List<IMemcachedNode>
            {
                CreateMockNode("10.0.0.3", 11211),
                CreateMockNode("10.0.0.4", 11211),
                CreateMockNode("10.0.0.5", 11211),
            };

            locator1.Initialize(nodesA);
            locator2.Initialize(nodesB);

            // Locator1 should only return nodes from nodesA
            for (int i = 0; i < 100; i++)
            {
                var result = locator1.Locate($"key-{i}");
                Assert.Contains(result, nodesA);
            }

            // Locator2 should only return nodes from nodesB
            for (int i = 0; i < 100; i++)
            {
                var result = locator2.Locate($"key-{i}");
                Assert.Contains(result, nodesB);
            }
        }
    }
}
