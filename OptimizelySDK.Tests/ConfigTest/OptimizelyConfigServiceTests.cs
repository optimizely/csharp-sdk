using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.OptlyConfig;

namespace OptimizelySDK.Tests.ConfigTest
{
    [TestFixture]
    internal class OptimizelyConfigServiceTests
    {
        [Test]
        public void TestNullProjectConfig()
        {
            var configService = new OptimizelyConfigService(null);

            Assert.IsNull(configService.GetOptimizelyConfig());
        }
    }
}