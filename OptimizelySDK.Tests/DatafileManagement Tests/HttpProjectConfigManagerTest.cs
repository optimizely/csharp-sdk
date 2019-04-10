
using Moq;
using NUnit.Framework;
using OptimizelySDK.DatafileManagement;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using System;
using System.Threading;
using System.Timers;

namespace OptimizelySDK.Tests.DatafileManagement_Tests
{
    [TestFixture]
    public class HttpProjectConfigManagerTest
    {
        // Project Id.
        private string SdkKey = "10192104166";
        private string Url = "https://cdn.optimizely.com/json/10192104166.json";
        private Mock<HttpProjectConfigManager> HttpConfigManagerMock;

        public Mock<HttpProjectConfigManager> GetHttpConfigManagerMock(string url, TimeSpan period, bool autoUpdate, ILogger logger, IErrorHandler errorHandler)
        {
            return new Mock<HttpProjectConfigManager>(url, period, autoUpdate, logger, errorHandler) { CallBase = true };
        }
        
        [Test]
        public void TestHttpConfigManagerReturnsCorrectProjectConfig()
        {
            PollingProjectConfigManager ConfigManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey(SdkKey)
                .WithAutoUpdate(true)
                .Build();
            
            var config = ConfigManager.GetConfig();
            Assert.NotNull(config);
        }

        [Test]
        public void TestHttpConfigManagerDoesNotPollContinouslyWhenAutoUpdateIsFalse()
        {
            HttpConfigManagerMock = GetHttpConfigManagerMock(Url, TimeSpan.FromMilliseconds(2000), false, null, null);
            HttpConfigManagerMock.Setup(mgr => mgr.FetchConfig());

            HttpConfigManagerMock.Object.GetConfig();
            Thread.Sleep(10000);

            // The timeout is 2 seconds but FetchConfig called once from GetConfig() as AutoUpdate is false.
            HttpConfigManagerMock.Verify(mgr => mgr.FetchConfig(), Times.Exactly(1));
        }

        [Test]
        public void TestHttpConfigManagerPollsContinouslyWhenAutoUpdateIsTrue()
        {
            HttpConfigManagerMock = GetHttpConfigManagerMock(Url, TimeSpan.FromMilliseconds(2000), true, null, null);
            HttpConfigManagerMock.Setup(mgr => mgr.FetchConfig());

            HttpConfigManagerMock.Object.GetConfig();
            
            Thread.Sleep(10000);

            // The timeout is 2 seconds so FetchConfig called continously from GetConfig() as AutoUpdate is true.
            HttpConfigManagerMock.Verify(mgr => mgr.FetchConfig(), Times.AtLeast(3));
        }
    }
}
