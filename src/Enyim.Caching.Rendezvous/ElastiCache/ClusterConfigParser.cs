// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;

namespace Enyim.Caching.Rendezvous.ElastiCache
{
    /// <summary>
    /// Parses the response from an AWS ElastiCache Memcached
    /// "config get cluster" command.
    ///
    /// Expected response format:
    /// <code>
    /// CONFIG cluster 0 {length}\r\n
    /// {version}\n
    /// host1|ip1|port1 host2|ip2|port2 ...\n\r\n
    /// END\r\n
    /// </code>
    /// </summary>
    public static class ClusterConfigParser
    {
        /// <summary>
        /// Parses a "config get cluster" response into a configuration version
        /// and list of cluster nodes.
        /// </summary>
        /// <param name="response">The raw response string from the config endpoint.</param>
        /// <returns>A tuple of (configVersion, nodes).</returns>
        public static (int Version, IReadOnlyList<ClusterNode> Nodes) Parse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                throw new ArgumentException("Response is empty or null.", nameof(response));

            // Normalize line endings and split into lines
            var normalized = response.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = normalized.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 3)
                throw new FormatException(
                    $"Expected at least 3 lines (header, version, nodes) in cluster config response. Got {lines.Length} line(s).");

            // Line 0: "CONFIG cluster 0 {length}" — skip
            // Line 1: version number
            if (!int.TryParse(lines[1].Trim(), out int version))
                throw new FormatException(
                    $"Expected integer config version on line 2, got: '{lines[1].Trim()}'");

            // Line 2: space-separated nodes, each as "hostname|ip|port"
            var nodeEntries = lines[2].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var nodes = new List<ClusterNode>(nodeEntries.Length);

            foreach (var entry in nodeEntries)
            {
                var parts = entry.Split('|');
                if (parts.Length != 3)
                    throw new FormatException(
                        $"Expected node format 'hostname|ip|port', got: '{entry}'");

                string hostName = parts[0];

                if (!IPAddress.TryParse(parts[1], out var ipAddress))
                    throw new FormatException(
                        $"Invalid IP address '{parts[1]}' in node entry: '{entry}'");

                if (!int.TryParse(parts[2], out int port) || port < 1 || port > 65535)
                    throw new FormatException(
                        $"Invalid port '{parts[2]}' in node entry: '{entry}'");

                nodes.Add(new ClusterNode(hostName, ipAddress, port));
            }

            return (version, nodes);
        }
    }
}
