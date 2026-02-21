// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Enyim.Caching.Rendezvous.Hashing
{
    /// <summary>
    /// Rendezvous hash implementation using MurmurHash3 (32-bit).
    /// Excellent distribution and performance for non-cryptographic hashing.
    /// </summary>
    public sealed class MurmurHash3RendezvousHash : IRendezvousHash
    {
        private readonly uint _seed;

        public MurmurHash3RendezvousHash() : this(0) { }

        public MurmurHash3RendezvousHash(uint seed)
        {
            _seed = seed;
        }

        public uint ComputeHash(string key, string node)
        {
            // Combine key + null separator + node to produce input
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var nodeBytes = Encoding.UTF8.GetBytes(node);
            var combined = new byte[keyBytes.Length + 1 + nodeBytes.Length];
            Buffer.BlockCopy(keyBytes, 0, combined, 0, keyBytes.Length);
            // combined[keyBytes.Length] is already 0 (separator)
            Buffer.BlockCopy(nodeBytes, 0, combined, keyBytes.Length + 1, nodeBytes.Length);

            return MurmurHash3_x86_32(combined, _seed);
        }

        private static uint MurmurHash3_x86_32(byte[] data, uint seed)
        {
            int length = data.Length;
            int nblocks = length / 4;

            uint h1 = seed;
            const uint c1 = 0xcc9e2d51;
            const uint c2 = 0x1b873593;

            // Body: process 4-byte blocks
            for (int i = 0; i < nblocks; i++)
            {
                int offset = i * 4;
                uint k1 = (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));

                k1 *= c1;
                k1 = RotateLeft(k1, 15);
                k1 *= c2;

                h1 ^= k1;
                h1 = RotateLeft(h1, 13);
                h1 = h1 * 5 + 0xe6546b64;
            }

            // Tail: process remaining bytes
            uint tail = 0;
            int tailIndex = nblocks * 4;
            switch (length & 3)
            {
                case 3:
                    tail ^= (uint)data[tailIndex + 2] << 16;
                    goto case 2;
                case 2:
                    tail ^= (uint)data[tailIndex + 1] << 8;
                    goto case 1;
                case 1:
                    tail ^= data[tailIndex];
                    tail *= c1;
                    tail = RotateLeft(tail, 15);
                    tail *= c2;
                    h1 ^= tail;
                    break;
            }

            // Finalization
            h1 ^= (uint)length;
            h1 = FMix32(h1);

            return h1;
        }

        private static uint RotateLeft(uint x, int r)
        {
            return (x << r) | (x >> (32 - r));
        }

        private static uint FMix32(uint h)
        {
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;
            return h;
        }
    }
}
