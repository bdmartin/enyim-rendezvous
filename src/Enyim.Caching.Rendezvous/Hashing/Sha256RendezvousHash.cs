// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Enyim.Caching.Rendezvous.Hashing
{
    /// <summary>
    /// Rendezvous hash implementation using SHA-256.
    /// Provides strong uniformity at the cost of higher CPU usage compared
    /// to non-cryptographic alternatives. Thread-safe.
    /// </summary>
    public sealed class Sha256RendezvousHash : IRendezvousHash
    {
        public uint ComputeHash(string key, string node)
        {
            var input = Encoding.UTF8.GetBytes(key + "\0" + node);

            byte[] hash;
            using (var sha256 = SHA256.Create())
            {
                hash = sha256.ComputeHash(input);
            }

            // Take the first 4 bytes of the SHA-256 digest as a uint32
            return (uint)(hash[0] | (hash[1] << 8) | (hash[2] << 16) | (hash[3] << 24));
        }
    }
}
