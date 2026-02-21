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
    }
}
