// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using Enyim.Caching.Memcached;
using Enyim.Caching.Rendezvous.Hashing;
using Moq;
using Xunit;

namespace Enyim.Caching.Rendezvous.Tests
{
    public class RendezvousNodeLocatorTests
    {
        private static IMemcachedNode CreateMockNode(string address, int port, bool isAlive = true)
        {
            var mock = new Mock<IMemcachedNode>();
            mock.Setup(n => n.EndPoint).Returns(new IPEndPoint(IPAddress.Parse(address), port));
            mock.Setup(n => n.IsAlive).Returns(isAlive);
            return mock.Object;
        }

        [Fact]
        public void Locate_WithSingleNode_ReturnsThatNode()
        {
            var node = CreateMockNode("10.0.0.1", 11211);
            var locator = new RendezvousNodeLocator();
            locator.Initialize(new List<IMemcachedNode> { node });

            var result = locator.Locate("mykey");

            Assert.Same(node, result);
        }

        [Fact]
        public void Locate_WithNoNodes_ReturnsNull()
        {
            var locator = new RendezvousNodeLocator();
            locator.Initialize(new List<IMemcachedNode>());

            var result = locator.Locate("mykey");

            Assert.Null(result);
        }

        [Fact]
        public void Locate_WithDeadSingleNode_ReturnsNull()
        {
            var node = CreateMockNode("10.0.0.1", 11211, isAlive: false);
            var locator = new RendezvousNodeLocator();
            locator.Initialize(new List<IMemcachedNode> { node });

            var result = locator.Locate("mykey");

            Assert.Null(result);
        }

        [Fact]
        public void Locate_SameKeyAlwaysReturnsSameNode()
        {
            var nodes = new List<IMemcachedNode>
            {
                CreateMockNode("10.0.0.1", 11211),
                CreateMockNode("10.0.0.2", 11211),
                CreateMockNode("10.0.0.3", 11211),
            };

            var locator = new RendezvousNodeLocator();
            locator.Initialize(nodes);

            var first = locator.Locate("consistent-key");

            // Call multiple times — should always return the same node
            for (int i = 0; i < 100; i++)
            {
                Assert.Same(first, locator.Locate("consistent-key"));
            }
        }

        [Fact]
        public void Locate_DifferentKeysCanMapToDifferentNodes()
        {
            var nodes = new List<IMemcachedNode>
            {
                CreateMockNode("10.0.0.1", 11211),
                CreateMockNode("10.0.0.2", 11211),
                CreateMockNode("10.0.0.3", 11211),
            };

            var locator = new RendezvousNodeLocator();
            locator.Initialize(nodes);

            var seenNodes = new HashSet<IMemcachedNode>();
            for (int i = 0; i < 1000; i++)
            {
                seenNodes.Add(locator.Locate($"key-{i}"));
            }

            // With 1000 keys and 3 nodes, all nodes should receive at least some keys
            Assert.Equal(3, seenNodes.Count);
        }

        [Fact]
        public void Locate_SkipsDeadNodes()
        {
            var alive1 = CreateMockNode("10.0.0.1", 11211, isAlive: true);
            var dead = CreateMockNode("10.0.0.2", 11211, isAlive: false);
            var alive2 = CreateMockNode("10.0.0.3", 11211, isAlive: true);

            var locator = new RendezvousNodeLocator();
            locator.Initialize(new List<IMemcachedNode> { alive1, dead, alive2 });

            for (int i = 0; i < 100; i++)
            {
                var result = locator.Locate($"key-{i}");
                Assert.NotSame(dead, result);
            }
        }

        [Fact]
        public void Locate_MinimalDisruption_WhenNodeAdded()
        {
            // Start with 3 nodes
            var node1 = CreateMockNode("10.0.0.1", 11211);
            var node2 = CreateMockNode("10.0.0.2", 11211);
            var node3 = CreateMockNode("10.0.0.3", 11211);

            var locator = new RendezvousNodeLocator();
            locator.Initialize(new List<IMemcachedNode> { node1, node2, node3 });

            // Record mappings for 1000 keys
            var originalMapping = new Dictionary<string, IMemcachedNode>();
            for (int i = 0; i < 1000; i++)
            {
                var key = $"key-{i}";
                originalMapping[key] = locator.Locate(key);
            }

            // Add a 4th node
            var node4 = CreateMockNode("10.0.0.4", 11211);
            locator.Initialize(new List<IMemcachedNode> { node1, node2, node3, node4 });

            // Count how many keys moved
            int movedCount = 0;
            for (int i = 0; i < 1000; i++)
            {
                var key = $"key-{i}";
                if (locator.Locate(key) != originalMapping[key])
                    movedCount++;
            }

            // With rendezvous hashing, ideally ~1/4 (25%) of keys should move to the new node.
            // Allow a generous range (15%-40%) to account for hash distribution variance.
            double movedPercent = movedCount / 10.0;
            Assert.InRange(movedPercent, 10.0, 45.0);
        }

        [Fact]
        public void Locate_MinimalDisruption_WhenNodeRemoved()
        {
            var node1 = CreateMockNode("10.0.0.1", 11211);
            var node2 = CreateMockNode("10.0.0.2", 11211);
            var node3 = CreateMockNode("10.0.0.3", 11211);

            var locator = new RendezvousNodeLocator();
            locator.Initialize(new List<IMemcachedNode> { node1, node2, node3 });

            var originalMapping = new Dictionary<string, IMemcachedNode>();
            for (int i = 0; i < 1000; i++)
            {
                var key = $"key-{i}";
                originalMapping[key] = locator.Locate(key);
            }

            // Remove node3
            locator.Initialize(new List<IMemcachedNode> { node1, node2 });

            int movedCount = 0;
            for (int i = 0; i < 1000; i++)
            {
                var key = $"key-{i}";
                var newNode = locator.Locate(key);

                if (originalMapping[key] == node3)
                {
                    // Keys that were on the removed node must move — not counted as disruption
                    continue;
                }

                if (newNode != originalMapping[key])
                    movedCount++;
            }

            // Keys that were NOT on the removed node should stay where they are
            Assert.Equal(0, movedCount);
        }

        [Fact]
        public void GetWorkingNodes_ReturnsOnlyAliveNodes()
        {
            var alive = CreateMockNode("10.0.0.1", 11211, isAlive: true);
            var dead = CreateMockNode("10.0.0.2", 11211, isAlive: false);

            var locator = new RendezvousNodeLocator();
            locator.Initialize(new List<IMemcachedNode> { alive, dead });

            var working = locator.GetWorkingNodes();
            Assert.Single(working);
            Assert.Contains(alive, working);
        }

        [Fact]
        public void Locate_NullKey_ThrowsArgumentNullException()
        {
            var locator = new RendezvousNodeLocator();
            locator.Initialize(new List<IMemcachedNode> { CreateMockNode("10.0.0.1", 11211) });

            Assert.Throws<ArgumentNullException>(() => locator.Locate(null));
        }

        [Fact]
        public void Initialize_NullNodes_ThrowsArgumentNullException()
        {
            var locator = new RendezvousNodeLocator();
            Assert.Throws<ArgumentNullException>(() => locator.Initialize(null));
        }

        [Theory]
        [InlineData(typeof(FnvRendezvousHash))]
        [InlineData(typeof(MurmurHash3RendezvousHash))]
        [InlineData(typeof(Sha256RendezvousHash))]
        public void Locate_WorksWithAllHashAlgorithms(Type hashType)
        {
            var hash = (IRendezvousHash)Activator.CreateInstance(hashType);
            var nodes = new List<IMemcachedNode>
            {
                CreateMockNode("10.0.0.1", 11211),
                CreateMockNode("10.0.0.2", 11211),
                CreateMockNode("10.0.0.3", 11211),
            };

            var locator = new RendezvousNodeLocator(hash);
            locator.Initialize(nodes);

            var result = locator.Locate("test-key");
            Assert.NotNull(result);

            // Deterministic
            Assert.Same(result, locator.Locate("test-key"));
        }

        [Fact]
        public void Locate_AllNodesDead_ReturnsNull()
        {
            var nodes = new List<IMemcachedNode>
            {
                CreateMockNode("10.0.0.1", 11211, isAlive: false),
                CreateMockNode("10.0.0.2", 11211, isAlive: false),
                CreateMockNode("10.0.0.3", 11211, isAlive: false),
            };

            var locator = new RendezvousNodeLocator();
            locator.Initialize(nodes);

            Assert.Null(locator.Locate("mykey"));
        }

        [Fact]
        public void Locate_AfterDispose_ThrowsObjectDisposedException()
        {
            var locator = new RendezvousNodeLocator();
            locator.Initialize(new List<IMemcachedNode> { CreateMockNode("10.0.0.1", 11211) });
            locator.Dispose();

            Assert.Throws<ObjectDisposedException>(() => locator.Locate("mykey"));
        }

        [Fact]
        public void Initialize_AfterDispose_ThrowsObjectDisposedException()
        {
            var locator = new RendezvousNodeLocator();
            locator.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
                locator.Initialize(new List<IMemcachedNode> { CreateMockNode("10.0.0.1", 11211) }));
        }

        [Fact]
        public void GetWorkingNodes_AfterDispose_ThrowsObjectDisposedException()
        {
            var locator = new RendezvousNodeLocator();
            locator.Dispose();

            Assert.Throws<ObjectDisposedException>(() => locator.GetWorkingNodes());
        }

        [Fact]
        public void Constructor_NullHash_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RendezvousNodeLocator(null));
        }
    }
}
