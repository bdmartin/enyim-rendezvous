// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Enyim.Caching.Rendezvous.ElastiCache
{
    /// <summary>
    /// Periodically polls an AWS ElastiCache Memcached configuration endpoint
    /// to discover cluster node changes. When nodes change, the
    /// <see cref="NodesChanged"/> event fires with the updated node list.
    ///
    /// Usage:
    /// <code>
    /// var options = new ElastiCacheDiscoveryOptions
    /// {
    ///     ConfigurationEndpoint = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 11211),
    ///     PollingInterval = TimeSpan.FromSeconds(60)
    /// };
    /// var discovery = new ElastiCacheDiscoveryService(options);
    /// discovery.NodesChanged += (sender, nodes) => { /* update your client config */ };
    /// discovery.Start();
    /// </code>
    /// </summary>
    public sealed class ElastiCacheDiscoveryService : IDisposable
    {
        private readonly ElastiCacheDiscoveryOptions _options;
        private Timer _pollTimer;
        private int _lastConfigVersion = -1;
        private IReadOnlyList<ClusterNode> _lastNodes = Array.Empty<ClusterNode>();
        private bool _disposed;

        /// <summary>
        /// Fires when the set of cluster nodes changes. The event args
        /// contain the newly discovered list of <see cref="ClusterNode"/> instances.
        /// </summary>
        public event EventHandler<ClusterNodesChangedEventArgs> NodesChanged;

        public ElastiCacheDiscoveryService(ElastiCacheDiscoveryOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (_options.ConfigurationEndpoint == null)
                throw new ArgumentException(
                    "ConfigurationEndpoint must be set.", nameof(options));
        }

        /// <summary>
        /// Gets the most recently discovered cluster nodes.
        /// </summary>
        public IReadOnlyList<ClusterNode> CurrentNodes => _lastNodes;

        /// <summary>
        /// Gets the most recently seen configuration version.
        /// Returns -1 if no successful poll has occurred yet.
        /// </summary>
        public int CurrentConfigVersion => _lastConfigVersion;

        /// <summary>
        /// Starts the background polling timer. Performs an initial poll immediately.
        /// </summary>
        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ElastiCacheDiscoveryService));

            // Poll once immediately, then on the interval
            _pollTimer = new Timer(
                _ => PollConfigEndpoint(),
                null,
                TimeSpan.Zero,
                _options.PollingInterval);
        }

        /// <summary>
        /// Stops the background polling timer.
        /// </summary>
        public void Stop()
        {
            _pollTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Performs a single poll of the ElastiCache configuration endpoint.
        /// Can be called manually for on-demand discovery.
        /// </summary>
        /// <returns>The list of discovered nodes, or null if the poll failed.</returns>
        public IReadOnlyList<ClusterNode> Poll()
        {
            return PollConfigEndpoint();
        }

        private IReadOnlyList<ClusterNode> PollConfigEndpoint()
        {
            try
            {
                string response = SendConfigCommand(_options.ConfigurationEndpoint);
                var (version, nodes) = ClusterConfigParser.Parse(response);

                if (version != _lastConfigVersion)
                {
                    _lastConfigVersion = version;
                    _lastNodes = nodes;
                    NodesChanged?.Invoke(this, new ClusterNodesChangedEventArgs(version, nodes));
                }

                return nodes;
            }
            catch
            {
                // Swallow exceptions during polling — the cluster continues
                // operating with the last known configuration.
                return null;
            }
        }

        private string SendConfigCommand(IPEndPoint endpoint)
        {
            using (var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.SendTimeout = (int)_options.ConnectionTimeout.TotalMilliseconds;
                socket.ReceiveTimeout = (int)_options.ReceiveTimeout.TotalMilliseconds;

                socket.Connect(endpoint);

                var command = Encoding.ASCII.GetBytes("config get cluster\r\n");
                socket.Send(command);

                var buffer = new byte[4096];
                var responseBuilder = new StringBuilder();

                while (true)
                {
                    int bytesRead = socket.Receive(buffer);
                    if (bytesRead == 0)
                        break;

                    responseBuilder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

                    // ElastiCache terminates the response with "END\r\n"
                    if (responseBuilder.ToString().Contains("END"))
                        break;
                }

                return responseBuilder.ToString();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _pollTimer?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Event args for the <see cref="ElastiCacheDiscoveryService.NodesChanged"/> event.
    /// </summary>
    public class ClusterNodesChangedEventArgs : EventArgs
    {
        public ClusterNodesChangedEventArgs(int configVersion, IReadOnlyList<ClusterNode> nodes)
        {
            ConfigVersion = configVersion;
            Nodes = nodes;
        }

        public int ConfigVersion { get; }
        public IReadOnlyList<ClusterNode> Nodes { get; }
    }
}
