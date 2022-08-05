using NUnit.Framework;

namespace OptimizelySDK.Tests.OdpTests
{
    [TestFixture]
    public class LruCacheTest
    {
        [SetUp]
        public void Setup() { }

        [Test]
        public void ShouldCreateSaveAndLookupOneItem() { }

        [Test]
        public void ShouldSaveAndLookupMultipleItems() { }
        
        [Test]
        public void ShouldHandleWhenCacheIsDisabled() { }
        
        [Test]
        public void ShouldThrowWhenItemsExpire() { }
        
        [Test]
        public void ShouldHandleWhenCacheReachesMaxSize() { }
        
        [Test]
        public void ShouldHandleWhenMaxSizeIsReducedInBetween() { }

        [Test]
        public void ShouldHandleWhenCacheIsReset() { }
    }
}