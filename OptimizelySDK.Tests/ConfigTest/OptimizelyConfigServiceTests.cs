using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.OptlyConfig;
using System.Diagnostics.CodeAnalysis;

namespace OptimizelySDK.Tests.ConfigTest
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
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