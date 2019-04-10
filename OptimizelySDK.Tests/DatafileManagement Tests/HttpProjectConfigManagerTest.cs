
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

        //public void Update

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

        //[Test]
        //public void TestHttpConfigManagerDoesNotPollContinouslyWhenAutoUpdateIsFalse()
        //{
        //    HttpConfigManagerMock = GetHttpConfigManagerMock(Url, TimeSpan.FromMilliseconds(5000), false, null, null);
        //    HttpConfigManagerMock.Setup(mgr => mgr.SetConfig(It.IsAny<DatafileManagement.ProjectConfig>()));

        //    //HttpConfigManagerMock.Object.GetConfig();
        //    Thread.Sleep(15000);

        //    HttpConfigManagerMock.Verify(mgr => mgr.SetConfig(It.IsAny<DatafileManagement.ProjectConfig>()), Times.Exactly(1));
        //}

        //[Test]
        //public void TestHttpConfigManagerPollsContinouslyWhenAutoUpdateIsTrue()
        //{
        //    HttpConfigManagerMock = GetHttpConfigManagerMock(Url, TimeSpan.FromMilliseconds(2000), true, null, null);
        //    HttpConfigManagerMock.Setup(mgr => mgr.Run(It.IsAny<object>(), It.IsAny<ElapsedEventArgs>()));

        //    var config = HttpConfigManagerMock.Object.GetConfig();
        //    Assert.NotNull(config);

        //    Thread.Sleep(15000);

        //    HttpConfigManagerMock.Verify(mgr => mgr.Run(It.IsAny<object>(), It.IsAny<ElapsedEventArgs>()), Times.AtLeast(3));
        //}
    }
}
