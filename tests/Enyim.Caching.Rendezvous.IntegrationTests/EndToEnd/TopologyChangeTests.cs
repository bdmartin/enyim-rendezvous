// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Enyim.Caching.Memcached;
using Moq;
using Xunit;

namespace Enyim.Caching.Rendezvous.IntegrationTests.EndToEnd
{
    public class TopologyChangeTests
    {
        private static IMemcachedNode CreateMockNode(string address, int port, bool isAlive = true)
        {
            var mock = new Mock<IMemcachedNode>();
            mock.Setup(n => n.EndPoint).Returns(new IPEndPoint(IPAddress.Parse(address), port));
            mock.Setup(n => n.IsAlive).Returns(isAlive);
            return mock.Object;
        }

        [Fact]
        public void ReInitialize_AddNode_OnlyNewNodeKeysMove()
        {
            var originalNodes = new List<IMemcachedNode>();
            for (int i = 1; i <= 5; i++)
                originalNodes.Add(CreateMockNode($"10.0.0.{i}", 11211));

            var locator = new RendezvousNodeLocator();
            locator.Initialize(originalNodes);

            // Record mappings for 10k keys
            var originalMapping = new Dictionary<string, IMemcachedNode>();
            for (int i = 0; i < 10000; i++)
            {
                var key = $"key-{i}";
                originalMapping[key] = locator.Locate(key);
            }

            // Add a 6th node
            var newNode = CreateMockNode("10.0.0.6", 11211);
            var expandedNodes = new List<IMemcachedNode>(originalNodes) { newNode };
            locator.Initialize(expandedNodes);

            int movedToNewNode = 0;
            int movedBetweenOriginals = 0;

            for (int i = 0; i < 10000; i++)
            {
                var key = $"key-{i}";
                var newLocation = locator.Locate(key);

                if (newLocation == originalMapping[key])
                    continue; // didn't move

                if (newLocation == newNode)
                    movedToNewNode++;
                else
                    movedBetweenOriginals++;
            }

            // Keys that moved should all go to the new node
            Assert.True(movedToNewNode > 0, "Expected some keys to move to the new node");
            Assert.Equal(0, movedBetweenOriginals);
        }

        [Fact]
        public void ReInitialize_RemoveNode_ZeroSpuriousMoves()
        {
            var nodes = new List<IMemcachedNode>();
            for (int i = 1; i <= 5; i++)
                nodes.Add(CreateMockNode($"10.0.0.{i}", 11211));

            var locator = new RendezvousNodeLocator();
            locator.Initialize(nodes);

            var originalMapping = new Dictionary<string, IMemcachedNode>();
            for (int i = 0; i < 10000; i++)
            {
                var key = $"key-{i}";
                originalMapping[key] = locator.Locate(key);
            }

            // Remove the last node
            var removedNode = nodes[4];
            var survivingNodes = nodes.Take(4).ToList();
            locator.Initialize(survivingNodes);

            int spuriousMoves = 0;
            for (int i = 0; i < 10000; i++)
            {
                var key = $"key-{i}";
                var newLocation = locator.Locate(key);

                // Skip keys that were on the removed node — they must move
                if (originalMapping[key] == removedNode)
                    continue;

                if (newLocation != originalMapping[key])
                    spuriousMoves++;
            }

            Assert.Equal(0, spuriousMoves);
        }

        [Fact]
        public void NodeHealthChange_DeadNodeRecovery()
        {
            var aliveFlag = new[] { true, true, true };
            var mocks = new List<Mock<IMemcachedNode>>();

            for (int i = 0; i < 3; i++)
            {
                var idx = i;
                var mock = new Mock<IMemcachedNode>();
                mock.Setup(n => n.EndPoint).Returns(new IPEndPoint(IPAddress.Parse($"10.0.0.{i + 1}"), 11211));
                mock.Setup(n => n.IsAlive).Returns(() => aliveFlag[idx]);
                mocks.Add(mock);
            }

            var nodes = mocks.Select(m => m.Object).ToList();
            var locator = new RendezvousNodeLocator();
            locator.Initialize(new List<IMemcachedNode>(nodes));

            // Record original mappings with all nodes alive
            var originalMapping = new Dictionary<string, IMemcachedNode>();
            for (int i = 0; i < 100; i++)
            {
                var key = $"key-{i}";
                originalMapping[key] = locator.Locate(key);
            }

            // Mark node 1 as dead
            aliveFlag[1] = false;

            // Locate should skip the dead node
            for (int i = 0; i < 100; i++)
            {
                var result = locator.Locate($"key-{i}");
                Assert.NotSame(nodes[1], result);
            }

            // Bring node 1 back alive and re-initialize
            aliveFlag[1] = true;
            locator.Initialize(new List<IMemcachedNode>(nodes));

            // Keys should return to original assignments
            for (int i = 0; i < 100; i++)
            {
                var key = $"key-{i}";
                Assert.Same(originalMapping[key], locator.Locate(key));
            }
        }

        [Fact]
        public void ReInitialize_EmptyToPopulated()
        {
            var locator = new RendezvousNodeLocator();
            locator.Initialize(new List<IMemcachedNode>());

            // Empty locator returns null
            Assert.Null(locator.Locate("any-key"));

            // Populate with nodes
            var nodes = new List<IMemcachedNode>
            {
                CreateMockNode("10.0.0.1", 11211),
                CreateMockNode("10.0.0.2", 11211),
                CreateMockNode("10.0.0.3", 11211),
            };

            locator.Initialize(nodes);

            // Now locate should work
            var result = locator.Locate("any-key");
            Assert.NotNull(result);
            Assert.Contains(result, nodes);
        }
    }
}
