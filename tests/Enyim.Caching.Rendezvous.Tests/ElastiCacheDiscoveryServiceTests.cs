// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Enyim.Caching.Rendezvous.ElastiCache;
using Xunit;

namespace Enyim.Caching.Rendezvous.Tests
{
    public class ElastiCacheDiscoveryOptionsTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var options = new ElastiCacheDiscoveryOptions();

            Assert.Equal(TimeSpan.FromSeconds(60), options.PollingInterval);
            Assert.Equal(TimeSpan.FromSeconds(5), options.ConnectionTimeout);
            Assert.Equal(TimeSpan.FromSeconds(5), options.ReceiveTimeout);
            Assert.Null(options.ConfigurationEndpoint);
        }

        [Fact]
        public void Properties_CanBeSet()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 11211);
            var options = new ElastiCacheDiscoveryOptions
            {
                ConfigurationEndpoint = endpoint,
                PollingInterval = TimeSpan.FromSeconds(30),
                ConnectionTimeout = TimeSpan.FromSeconds(10),
                ReceiveTimeout = TimeSpan.FromSeconds(15)
            };

            Assert.Same(endpoint, options.ConfigurationEndpoint);
            Assert.Equal(TimeSpan.FromSeconds(30), options.PollingInterval);
            Assert.Equal(TimeSpan.FromSeconds(10), options.ConnectionTimeout);
            Assert.Equal(TimeSpan.FromSeconds(15), options.ReceiveTimeout);
        }
    }

    public class ClusterNodesChangedEventArgsTests
    {
        [Fact]
        public void Constructor_SetsProperties()
        {
            var nodes = new[]
            {
                new ClusterNode("host.com", IPAddress.Parse("10.0.0.1"), 11211)
            };

            var args = new ClusterNodesChangedEventArgs(42, nodes);

            Assert.Equal(42, args.ConfigVersion);
            Assert.Same(nodes, args.Nodes);
        }
    }

    public class ElastiCacheDiscoveryServiceTests : IDisposable
    {
        private TcpListener _listener;

        private IPEndPoint StartListener(string response)
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

                            var data = Encoding.ASCII.GetBytes(response);
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
                catch (InvalidOperationException)
                {
                    // Listener was stopped before AcceptTcpClient returned
                }
            });

            return endpoint;
        }

        public void Dispose()
        {
            _listener?.Stop();
        }

        [Fact]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ElastiCacheDiscoveryService(null));
        }

        [Fact]
        public void Constructor_NullEndpoint_ThrowsArgumentException()
        {
            var options = new ElastiCacheDiscoveryOptions();
            Assert.Throws<ArgumentException>(() => new ElastiCacheDiscoveryService(options));
        }

        [Fact]
        public void Constructor_ValidOptions_InitialState()
        {
            var options = new ElastiCacheDiscoveryOptions
            {
                ConfigurationEndpoint = new IPEndPoint(IPAddress.Loopback, 11211)
            };

            using (var service = new ElastiCacheDiscoveryService(options))
            {
                Assert.Empty(service.CurrentNodes);
                Assert.Equal(-1, service.CurrentConfigVersion);
                Assert.Null(service.LastError);
            }
        }

        [Fact]
        public void Start_AfterDispose_ThrowsObjectDisposedException()
        {
            var options = new ElastiCacheDiscoveryOptions
            {
                ConfigurationEndpoint = new IPEndPoint(IPAddress.Loopback, 11211)
            };

            var service = new ElastiCacheDiscoveryService(options);
            service.Dispose();

            Assert.Throws<ObjectDisposedException>(() => service.Start());
        }

        [Fact]
        public void Dispose_IsIdempotent()
        {
            var options = new ElastiCacheDiscoveryOptions
            {
                ConfigurationEndpoint = new IPEndPoint(IPAddress.Loopback, 11211)
            };

            var service = new ElastiCacheDiscoveryService(options);
            service.Dispose();
            service.Dispose(); // Should not throw
        }

        [Fact]
        public void Poll_ValidEndpoint_UpdatesStateAndFiresEvent()
        {
            var response =
                "CONFIG cluster 0 136\r\n" +
                "12\n" +
                "myCluster.0001.cache.amazonaws.com|10.82.235.120|11211 myCluster.0002.cache.amazonaws.com|10.80.249.27|11211\n\r\n" +
                "END\r\n";

            var endpoint = StartListener(response);

            var options = new ElastiCacheDiscoveryOptions
            {
                ConfigurationEndpoint = endpoint,
                ConnectionTimeout = TimeSpan.FromSeconds(5),
                ReceiveTimeout = TimeSpan.FromSeconds(5)
            };

            using (var service = new ElastiCacheDiscoveryService(options))
            {
                ClusterNodesChangedEventArgs receivedArgs = null;
                service.NodesChanged += (sender, args) => receivedArgs = args;

                var result = service.Poll();

                Assert.NotNull(result);
                Assert.Equal(2, result.Count);
                Assert.Equal(12, service.CurrentConfigVersion);
                Assert.Equal(2, service.CurrentNodes.Count);
                Assert.Null(service.LastError);

                Assert.NotNull(receivedArgs);
                Assert.Equal(12, receivedArgs.ConfigVersion);
                Assert.Equal(2, receivedArgs.Nodes.Count);
            }
        }

        [Fact]
        public void Poll_ClosedPort_SetsLastError()
        {
            // Bind to get an OS-assigned port, then stop to ensure nothing is listening
            var tempListener = new TcpListener(IPAddress.Loopback, 0);
            tempListener.Start();
            var endpoint = (IPEndPoint)tempListener.LocalEndpoint;
            tempListener.Stop();

            var options = new ElastiCacheDiscoveryOptions
            {
                ConfigurationEndpoint = endpoint,
                ConnectionTimeout = TimeSpan.FromSeconds(2),
                ReceiveTimeout = TimeSpan.FromSeconds(2)
            };

            using (var service = new ElastiCacheDiscoveryService(options))
            {
                var result = service.Poll();

                Assert.Null(result);
                Assert.NotNull(service.LastError);
            }
        }

        [Fact]
        public void Poll_SameVersion_DoesNotFireEventAgain()
        {
            var response =
                "CONFIG cluster 0 80\r\n" +
                "1\n" +
                "host.cache.amazonaws.com|10.0.0.1|11211\n\r\n" +
                "END\r\n";

            var endpoint = StartListener(response);

            var options = new ElastiCacheDiscoveryOptions
            {
                ConfigurationEndpoint = endpoint,
                ConnectionTimeout = TimeSpan.FromSeconds(5),
                ReceiveTimeout = TimeSpan.FromSeconds(5)
            };

            using (var service = new ElastiCacheDiscoveryService(options))
            {
                int eventCount = 0;
                service.NodesChanged += (sender, args) => Interlocked.Increment(ref eventCount);

                service.Poll();
                Assert.Equal(1, eventCount);

                service.Poll();
                Assert.Equal(1, eventCount); // Should not fire again for same version
            }
        }
    }
}
