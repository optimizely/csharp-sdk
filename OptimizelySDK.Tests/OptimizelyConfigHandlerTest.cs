using NUnit.Framework;
using OptimizelySDK.Config;
using System.Configuration;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class OptimizelyConfigHandlerTest
    {
        [Test]
        public void TestHTTPAppConfigSection()
        {
            var configSection = ConfigurationManager.GetSection("optlySDKConfigSection") as OptimizelySDKConfigSection;
            var httpSetting = configSection.HttpProjectConfig;
            Assert.IsNotNull(httpSetting);
            Assert.IsTrue(httpSetting.AutoUpdate);
            Assert.AreEqual(httpSetting.BlockingTimeOutInMs, 10000);
            Assert.AreEqual(httpSetting.DatafileUrlFormat, "https://cdn.optimizely.com/data/{0}.json");
            Assert.IsTrue(httpSetting.DefaultStart);
            Assert.AreEqual(httpSetting.PollingIntervalInMs, 2000);
            Assert.AreEqual(httpSetting.SDKKey, "43214321");
            Assert.AreEqual(httpSetting.Url, "www.testurl.com");
        }

        [Test]
        public void TestBatchEventAppConfigSection()
        {
            var configSection = ConfigurationManager.GetSection("optlySDKConfigSection") as OptimizelySDKConfigSection;
            var batchSetting = configSection.BatchEventProcessor;
            Assert.IsNotNull(batchSetting);
            Assert.AreEqual(batchSetting.BatchSize, 10);
            Assert.AreEqual(batchSetting.FlushIntervalInMs, 2000);
            Assert.AreEqual(batchSetting.TimeoutIntervalInMs, 10000);
            Assert.IsTrue(batchSetting.DefaultStart);
        }
    }
}
