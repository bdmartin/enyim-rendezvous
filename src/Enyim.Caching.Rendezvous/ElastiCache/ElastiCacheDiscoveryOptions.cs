// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Net;

namespace Enyim.Caching.Rendezvous.ElastiCache
{
    /// <summary>
    /// Configuration options for ElastiCache cluster auto-discovery.
    /// </summary>
    public class ElastiCacheDiscoveryOptions
    {
        /// <summary>
        /// The configuration endpoint of the ElastiCache cluster.
        /// This is the endpoint that responds to "config get cluster" commands.
        /// </summary>
        public IPEndPoint ConfigurationEndpoint { get; set; }

        /// <summary>
        /// How frequently to poll the configuration endpoint for node changes.
        /// Default is 60 seconds.
        /// </summary>
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Timeout for connecting to the configuration endpoint.
        /// Default is 5 seconds.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Timeout for receiving data from the configuration endpoint.
        /// Default is 5 seconds.
        /// </summary>
        public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }
}
