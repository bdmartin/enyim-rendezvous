using NUnit.Framework;

namespace Enyim.Rendezvous.NodeLocator
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ConstructorTest()
        {
            // Arrange
            IMemcachedKeyTransformer keyTransformer = new Base64KeyTransformer();

            // Act
            IMemcachedNodeLocator nodeLocator = new RendezvousNodeLocator(keyTransformer);

            // Assert
            Assert.
        }
    }
}