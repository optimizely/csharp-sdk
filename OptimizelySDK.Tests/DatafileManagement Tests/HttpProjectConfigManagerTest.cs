
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
        //private string Url = "https://cdn.optimizely.com/json/10192104166.json";
        private Mock<HttpProjectConfigManager> HttpConfigManagerMock;
        private Mock<ILogger> LoggerMock = new Mock<ILogger>();

        //public Mock<HttpProjectConfigManager> GetHttpConfigManagerMock(string sdkKey, TimeSpan period, bool autoUpdate, ILogger logger, IErrorHandler errorHandler)
        //{
        //    string url = $"https://cdn.optimizely.com/json/{sdkKey}.json";
        //    HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
        //        .WithSdkKey(sdkKey)
        //        .WithAutoUpdate(true)
        //        .WithLogger(LoggerMock.Object)
        //        .Build();

        //    return new Mock<PollingProjectConfigManager>(sdkKey, period, autoUpdate, logger, errorHandler, httpManager) { CallBase = true };
        //}
        
        [Test]
        public void TestHttpConfigManagerReturnsCorrectProjectConfig()
        {
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey(SdkKey)
                .WithAutoUpdate(true)
                .WithLogger(LoggerMock.Object)
                .Build();


            var configManager = new PollingProjectConfigManager(TimeSpan.FromSeconds(5), true, null, null, httpManager);
            var config = configManager.GetConfig();
            Assert.NotNull(config);
        }

        //[Test]
        //public void TestHttpConfigManagerDoesNotPollContinouslyWhenAutoUpdateIsFalse()
        //{
        //    HttpConfigManagerMock = GetHttpConfigManagerMock(SdkKey, TimeSpan.FromMilliseconds(2000), false, null, null);
        //    HttpConfigManagerMock.Setup(mgr => mgr.FetchConfig());

        //    HttpConfigManagerMock.Object.GetConfig();
        //    Thread.Sleep(10000);

        //    // The timeout is 2 seconds but FetchConfig called once from GetConfig() as AutoUpdate is false.
        //    HttpConfigManagerMock.Verify(mgr => mgr.FetchConfig(), Times.Exactly(1));
        //}

        //[Test]
        //public void TestHttpConfigManagerPollsContinouslyWhenAutoUpdateIsTrue()
        //{
        //    HttpConfigManagerMock = GetHttpConfigManagerMock(SdkKey, TimeSpan.FromMilliseconds(2000), true, null, null);
        //    HttpConfigManagerMock.Setup(mgr => mgr.FetchConfig());

        //    HttpConfigManagerMock.Object.GetConfig();
            
        //    Thread.Sleep(10000);

        //    // The timeout is 2 seconds so FetchConfig called continously from GetConfig() as AutoUpdate is true.
        //    HttpConfigManagerMock.Verify(mgr => mgr.FetchConfig(), Times.AtLeast(3));
        //}
    }
}
