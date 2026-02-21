// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Enyim.Caching.Memcached;
using Enyim.Caching.Rendezvous.Hashing;

namespace Enyim.Caching.Rendezvous
{
    /// <summary>
    /// An <see cref="IMemcachedNodeLocator"/> that uses Rendezvous hashing
    /// (Highest Random Weight / HRW) to map keys to memcached nodes.
    ///
    /// Unlike consistent hashing (Ketama), Rendezvous hashing does not require
    /// a hash ring or virtual nodes. For each key, every node is scored and the
    /// node with the highest score is selected. When a node is added or removed,
    /// only keys that map to that node are redistributed — giving the same
    /// minimal-disruption guarantee as consistent hashing with simpler code.
    /// </summary>
    public sealed class RendezvousNodeLocator : IMemcachedNodeLocator
    {
        private readonly IRendezvousHash _hash;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        private IMemcachedNode[] _allNodes = Array.Empty<IMemcachedNode>();
        private string[] _nodeEndpoints = Array.Empty<string>();
        private bool _disposed;

        /// <summary>
        /// Creates a new locator using the default FNV-1a hash algorithm.
        /// </summary>
        public RendezvousNodeLocator() : this(new FnvRendezvousHash()) { }

        /// <summary>
        /// Creates a new locator with a custom hash algorithm.
        /// </summary>
        /// <param name="hash">The hash algorithm used to score key-node pairs.</param>
        public RendezvousNodeLocator(IRendezvousHash hash)
        {
            _hash = hash ?? throw new ArgumentNullException(nameof(hash));
        }

        /// <inheritdoc />
        public void Initialize(IList<IMemcachedNode> nodes)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RendezvousNodeLocator));
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));

            _lock.EnterWriteLock();
            try
            {
                _allNodes = new IMemcachedNode[nodes.Count];
                _nodeEndpoints = new string[nodes.Count];

                for (int i = 0; i < nodes.Count; i++)
                {
                    _allNodes[i] = nodes[i];
                    _nodeEndpoints[i] = nodes[i].EndPoint.ToString();
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <inheritdoc />
        public IMemcachedNode Locate(string key)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RendezvousNodeLocator));
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _lock.EnterReadLock();
            try
            {
                var nodes = _allNodes;
                var endpoints = _nodeEndpoints;

                if (nodes.Length == 0)
                    return null;

                if (nodes.Length == 1)
                    return nodes[0].IsAlive ? nodes[0] : null;

                IMemcachedNode bestNode = null;
                uint bestScore = 0;

                for (int i = 0; i < nodes.Length; i++)
                {
                    if (!nodes[i].IsAlive)
                        continue;

                    uint score = _hash.ComputeHash(key, endpoints[i]);

                    if (bestNode == null || score > bestScore)
                    {
                        bestScore = score;
                        bestNode = nodes[i];
                    }
                }

                return bestNode;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <inheritdoc />
        public IEnumerable<IMemcachedNode> GetWorkingNodes()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RendezvousNodeLocator));
            _lock.EnterReadLock();
            try
            {
                var nodes = _allNodes;
                var result = new List<IMemcachedNode>(nodes.Length);

                for (int i = 0; i < nodes.Length; i++)
                {
                    if (nodes[i].IsAlive)
                        result.Add(nodes[i]);
                }

                return result;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _lock?.Dispose();
                _disposed = true;
            }
        }
    }
}
