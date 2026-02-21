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
        public ClusterNode(string hostName, IPAddress ipAddress, int port)
        {
            HostName = hostName ?? throw new ArgumentNullException(nameof(hostName));
            IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            Port = port;
            EndPoint = new IPEndPoint(ipAddress, port);
        }

        public string HostName { get; }
        public IPAddress IpAddress { get; }
        public int Port { get; }
        public IPEndPoint EndPoint { get; }

        public bool Equals(ClusterNode other)
        {
            if (other == null) return false;
            return string.Equals(HostName, other.HostName, StringComparison.OrdinalIgnoreCase)
                && IpAddress.Equals(other.IpAddress)
                && Port == other.Port;
        }

        public override bool Equals(object obj) => Equals(obj as ClusterNode);

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

        public override string ToString() => $"{HostName}|{IpAddress}|{Port}";
    }
}
