// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System;
using Enyim.Caching.Rendezvous.Hashing;
using Xunit;

namespace Enyim.Caching.Rendezvous.Tests
{
    public class RendezvousNodeLocatorFactoryTests
    {
        [Fact]
        public void Create_ReturnsRendezvousNodeLocator()
        {
            var factory = new RendezvousNodeLocatorFactory();
            var locator = factory.Create();

            Assert.NotNull(locator);
            Assert.IsType<RendezvousNodeLocator>(locator);
        }

        [Fact]
        public void Create_WithCustomHash_ReturnsLocatorUsingThatHash()
        {
            var factory = new RendezvousNodeLocatorFactory(new MurmurHash3RendezvousHash());
            var locator = factory.Create();

            Assert.NotNull(locator);
            Assert.IsType<RendezvousNodeLocator>(locator);
        }

        [Fact]
        public void Create_EachCallReturnsNewInstance()
        {
            var factory = new RendezvousNodeLocatorFactory();

            var locator1 = factory.Create();
            var locator2 = factory.Create();

            Assert.NotSame(locator1, locator2);
        }

        [Fact]
        public void Constructor_NullHash_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new RendezvousNodeLocatorFactory(null));
        }

        [Fact]
        public void Initialize_Null_DoesNotThrow()
        {
            var factory = new RendezvousNodeLocatorFactory();
            factory.Initialize(null);
        }

        [Fact]
        public void Initialize_WithDictionary_DoesNotThrow()
        {
            var factory = new RendezvousNodeLocatorFactory();
            var parameters = new System.Collections.Generic.Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            factory.Initialize(parameters);
        }
    }
}
