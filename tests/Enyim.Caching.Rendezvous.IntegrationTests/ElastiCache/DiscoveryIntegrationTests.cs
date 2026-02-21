// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Enyim.Caching.Memcached;
using Enyim.Caching.Rendezvous.ElastiCache;
using Moq;
using Xunit;

namespace Enyim.Caching.Rendezvous.IntegrationTests.ElastiCache
{
    public class DiscoveryIntegrationTests : IDisposable
    {
        private TcpListener _listener;
        private volatile string _currentResponse;

        private IPEndPoint StartListener()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            var endpoint = (IPEndPoint)_listener.LocalEndpoint;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    while (true)
                    {
                        using (var client = _listener.AcceptTcpClient())
                        using (var stream = client.GetStream())
                        {
                            var buffer = new byte[1024];
                            stream.Read(buffer, 0, buffer.Length);

                            var data = Encoding.ASCII.GetBytes(_currentResponse);
                            stream.Write(data, 0, data.Length);
                            client.Close();
                        }
                    }
                }
                catch (SocketException)
                {
                    // Listener was stopped
                }
                catch (ObjectDisposedException)
                {
                    // Listener was disposed
                }
            });

            return endpoint;
        }

        public void Dispose()
        {
            _listener?.Stop();
        }

        [Fact]
        public void Discovery_ToLocator_FullPipeline()
        {
            _currentResponse =
                "CONFIG cluster 0 136\r\n" +
                "1\n" +
                "node1.cache.amazonaws.com|10.0.0.1|11211 node2.cache.amazonaws.com|10.0.0.2|11211 node3.cache.amazonaws.com|10.0.0.3|11211\n\r\n" +
                "END\r\n";

            var endpoint = StartListener();

            var options = new ElastiCacheDiscoveryOptions
            {
                ConfigurationEndpoint = endpoint,
                ConnectionTimeout = TimeSpan.FromSeconds(5),
                ReceiveTimeout = TimeSpan.FromSeconds(5)
            };

            using (var service = new ElastiCacheDiscoveryService(options))
            {
                var result = service.Poll();

                Assert.NotNull(result);
                Assert.Equal(3, result.Count);

                // Create mock nodes from discovered ClusterNodes
                var mockNodes = result.Select(cn =>
                    CreateMockNode(cn.IpAddress.ToString(), cn.Port)).ToList();

                var locator = new RendezvousNodeLocator();
                locator.Initialize(new List<IMemcachedNode>(mockNodes));

                // Verify the locator works with discovered nodes
                var mappings = new Dictionary<string, IMemcachedNode>();
                for (int i = 0; i < 100; i++)
                {
                    var key = $"key-{i}";
                    mappings[key] = locator.Locate(key);
                    Assert.NotNull(mappings[key]);
                }

                // All nodes should be used
                var usedNodes = new HashSet<IMemcachedNode>(mappings.Values);
                Assert.Equal(3, usedNodes.Count);
            }
        }

        [Fact]
        public void Discovery_ConfigVersionChange_LocatorReInitialized()
        {
            _currentResponse =
                "CONFIG cluster 0 80\r\n" +
                "1\n" +
                "node1.cache.amazonaws.com|10.0.0.1|11211 node2.cache.amazonaws.com|10.0.0.2|11211\n\r\n" +
                "END\r\n";

            var endpoint = StartListener();

            var options = new ElastiCacheDiscoveryOptions
            {
                ConfigurationEndpoint = endpoint,
                ConnectionTimeout = TimeSpan.FromSeconds(5),
                ReceiveTimeout = TimeSpan.FromSeconds(5)
            };

            using (var service = new ElastiCacheDiscoveryService(options))
            {
                int eventCount = 0;
                IReadOnlyList<ClusterNode> latestNodes = null;

                service.NodesChanged += (sender, args) =>
                {
                    Interlocked.Increment(ref eventCount);
                    latestNodes = args.Nodes;
                };

                // First poll: version 1, 2 nodes
                var result1 = service.Poll();
                Assert.NotNull(result1);
                Assert.Equal(2, result1.Count);
                Assert.Equal(1, eventCount);

                // Update to version 2 with 3 nodes
                _currentResponse =
                    "CONFIG cluster 0 136\r\n" +
                    "2\n" +
                    "node1.cache.amazonaws.com|10.0.0.1|11211 node2.cache.amazonaws.com|10.0.0.2|11211 node3.cache.amazonaws.com|10.0.0.3|11211\n\r\n" +
                    "END\r\n";

                var result2 = service.Poll();
                Assert.NotNull(result2);
                Assert.Equal(3, result2.Count);
                Assert.Equal(2, eventCount);

                // Build locator from latest nodes — should have 3 nodes
                var mockNodes = latestNodes.Select(cn =>
                    CreateMockNode(cn.IpAddress.ToString(), cn.Port)).ToList();

                var locator = new RendezvousNodeLocator();
                locator.Initialize(new List<IMemcachedNode>(mockNodes));

                // Locate should distribute across all 3 nodes
                var usedNodes = new HashSet<IMemcachedNode>();
                for (int i = 0; i < 1000; i++)
                    usedNodes.Add(locator.Locate($"key-{i}"));

                Assert.Equal(3, usedNodes.Count);
            }
        }

        [Fact]
        public void Discovery_NetworkFailure_LocatorRetainsPreviousState()
        {
            _currentResponse =
                "CONFIG cluster 0 80\r\n" +
                "1\n" +
                "node1.cache.amazonaws.com|10.0.0.1|11211 node2.cache.amazonaws.com|10.0.0.2|11211\n\r\n" +
                "END\r\n";

            var endpoint = StartListener();

            var options = new ElastiCacheDiscoveryOptions
            {
                ConfigurationEndpoint = endpoint,
                ConnectionTimeout = TimeSpan.FromSeconds(2),
                ReceiveTimeout = TimeSpan.FromSeconds(2)
            };

            using (var service = new ElastiCacheDiscoveryService(options))
            {
                // First poll succeeds
                var result = service.Poll();
                Assert.NotNull(result);
                Assert.Equal(2, result.Count);
                Assert.Null(service.LastError);

                var nodesBeforeFailure = service.CurrentNodes;

                // Stop the listener to simulate network failure
                _listener.Stop();

                // Second poll should fail
                var failedResult = service.Poll();
                Assert.Null(failedResult);
                Assert.NotNull(service.LastError);

                // CurrentNodes should still have the previous state
                Assert.Equal(2, service.CurrentNodes.Count);
                Assert.Same(nodesBeforeFailure, service.CurrentNodes);
            }
        }

        private static IMemcachedNode CreateMockNode(string address, int port, bool isAlive = true)
        {
            var mock = new Mock<IMemcachedNode>();
            mock.Setup(n => n.EndPoint).Returns(new IPEndPoint(IPAddress.Parse(address), port));
            mock.Setup(n => n.IsAlive).Returns(isAlive);
            return mock.Object;
        }
    }
}
