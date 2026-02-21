// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System.Text;

namespace Enyim.Caching.Rendezvous.Hashing
{
    /// <summary>
    /// Rendezvous hash implementation using FNV-1a (Fowler-Noll-Vo) 32-bit hash.
    /// This is the default hash algorithm — fast, non-cryptographic, with good distribution.
    /// </summary>
    public sealed class FnvRendezvousHash : IRendezvousHash
    {
        private const uint FnvOffsetBasis = 2166136261;
        private const uint FnvPrime = 16777619;

        public uint ComputeHash(string key, string node)
        {
            uint hash = FnvOffsetBasis;

            // Hash the key bytes
            var keyBytes = Encoding.UTF8.GetBytes(key);
            for (int i = 0; i < keyBytes.Length; i++)
            {
                hash ^= keyBytes[i];
                hash *= FnvPrime;
            }

            // Separator to prevent collisions between key/node boundaries
            hash ^= 0;
            hash *= FnvPrime;

            // Hash the node bytes
            var nodeBytes = Encoding.UTF8.GetBytes(node);
            for (int i = 0; i < nodeBytes.Length; i++)
            {
                hash ^= nodeBytes[i];
                hash *= FnvPrime;
            }

            return hash;
        }
    }
}
