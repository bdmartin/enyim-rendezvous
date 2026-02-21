// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Net;

namespace Enyim.Caching.Rendezvous.ElastiCache
{
    /// <summary>
    /// Represents a node discovered from an AWS ElastiCache cluster configuration endpoint.
    /// </summary>
    public sealed class ClusterNode : IEquatable<ClusterNode>
    {
        /// <summary>
        /// Initializes a new <see cref="ClusterNode"/> with the given host, IP address, and port.
        /// </summary>
        /// <param name="hostName">The DNS host name of the cache node.</param>
        /// <param name="ipAddress">The resolved IP address of the cache node.</param>
        /// <param name="port">The memcached port number.</param>
        public ClusterNode(string hostName, IPAddress ipAddress, int port)
        {
            HostName = hostName ?? throw new ArgumentNullException(nameof(hostName));
            IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            Port = port;
            EndPoint = new IPEndPoint(ipAddress, port);
        }

        /// <summary>Gets the DNS host name of the cache node.</summary>
        public string HostName { get; }

        /// <summary>Gets the resolved IP address of the cache node.</summary>
        public IPAddress IpAddress { get; }

        /// <summary>Gets the memcached port number.</summary>
        public int Port { get; }

        /// <summary>Gets the endpoint (IP + port) for connecting to this node.</summary>
        public IPEndPoint EndPoint { get; }

        /// <inheritdoc />
        public bool Equals(ClusterNode other)
        {
            if (other == null) return false;
            return string.Equals(HostName, other.HostName, StringComparison.OrdinalIgnoreCase)
                && IpAddress.Equals(other.IpAddress)
                && Port == other.Port;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as ClusterNode);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(HostName);
                hash = hash * 31 + IpAddress.GetHashCode();
                hash = hash * 31 + Port;
                return hash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{HostName}|{IpAddress}|{Port}";
    }
}
