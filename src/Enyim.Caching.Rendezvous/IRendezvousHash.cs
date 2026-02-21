// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

namespace Enyim.Caching.Rendezvous
{
    /// <summary>
    /// Defines a hash function used by the Rendezvous (HRW) node locator
    /// to compute a score for a given key-node pair.
    /// </summary>
    public interface IRendezvousHash
    {
        /// <summary>
        /// Computes a hash score for the given key and node identifier.
        /// The node with the highest score for a given key wins ownership.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="node">A string identifying the cache node (typically its endpoint address).</param>
        /// <returns>A 32-bit unsigned hash value used as the weight/score.</returns>
        uint ComputeHash(string key, string node);
    }
}
