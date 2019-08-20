using NUnit.Framework;
using OptimizelySDK.Config;
using System.Configuration;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class OptimizelyConfigSectiontest
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void TestConfigSection()
        {
            var configSection = ConfigurationManager.GetSection("optlySDKConfigSection") as OptimizelySDKConfigSection;
            var httpSettings = configSection.HttpProjectConfig;
            var eventBatchSize = configSection.BatchEventProcessor;
            Assert.IsNotNull(configSection);
        }
    }
}