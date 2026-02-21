// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Enyim.Caching.Memcached;
using Enyim.Caching.Rendezvous.Hashing;

namespace Enyim.Caching.Rendezvous
{
    /// <summary>
    /// Factory that creates <see cref="RendezvousNodeLocator"/> instances
    /// for use with the Enyim memcached client.
    ///
    /// Hash algorithm selection is done via the constructor. Use the
    /// parameterized constructor to inject a custom <see cref="IRendezvousHash"/>.
    /// The <see cref="Initialize"/> method exists for interface compliance only.
    /// </summary>
    public class RendezvousNodeLocatorFactory : IProviderFactory<IMemcachedNodeLocator>
    {
        private readonly IRendezvousHash _hash;

        /// <summary>
        /// Creates a factory that uses the default FNV-1a hash algorithm.
        /// </summary>
        public RendezvousNodeLocatorFactory() : this(new FnvRendezvousHash()) { }

        /// <summary>
        /// Creates a factory that uses the specified hash algorithm.
        /// </summary>
        public RendezvousNodeLocatorFactory(IRendezvousHash hash)
        {
            _hash = hash ?? throw new ArgumentNullException(nameof(hash));
        }

        public IMemcachedNodeLocator Create()
        {
            return new RendezvousNodeLocator(_hash);
        }

        public void Initialize(Dictionary<string, string> parameters)
        {
            // When used via config-based initialization, the hash algorithm
            // is determined by the constructor. This method is intentionally
            // left empty for interface compliance; use the parameterized
            // constructor to select a hash algorithm.
        }
    }
}
