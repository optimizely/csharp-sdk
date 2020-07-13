/*
 * Copyright 2019-2020, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Moq;
using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.Logger;
using OptimizelySDK.Tests.NotificationTests;
using OptimizelySDK.Tests.Utils;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizelySDK.Tests.DatafileManagement_Tests
{
    [TestFixture]
    public class HttpProjectConfigManagerTest
    {
        private Mock<ILogger> LoggerMock;
        private Mock<HttpProjectConfigManager.HttpClient> HttpClientMock;
        private Mock<TestNotificationCallbacks> NotificationCallbackMock = new Mock<TestNotificationCallbacks>();

        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            HttpClientMock = new Mock<HttpProjectConfigManager.HttpClient>();
            HttpClientMock.Reset();
            TestHttpProjectConfigManagerUtil.SetClientFieldValue(HttpClientMock.Object);
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            NotificationCallbackMock.Setup(nc => nc.TestConfigUpdateCallback());

        }

        [Test]
        public void TestHttpConfigManagerRetreiveProjectConfigByURL()
        {
            var t = MockSendAsync(TestData.Datafile);
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithUrl("https://cdn.optimizely.com/datafiles/QBw9gFM8oTn7ogY9ANCC1z.json")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .WithStartByDefault()
                .Build();

            // This method waits until SendAsync is not triggered.
            // Time is given here to avoid hanging-up in any worst case.
            t.Wait(1000);

            HttpClientMock.Verify(_ => _.SendAsync(
                It.Is<System.Net.Http.HttpRequestMessage>(requestMessage =>
                requestMessage.RequestUri.ToString() == "https://cdn.optimizely.com/datafiles/QBw9gFM8oTn7ogY9ANCC1z.json"
                )));
            httpManager.Dispose();
        }

        [Test]
        public void TestHttpConfigManagerWithInvalidStatus()
        {
            var t = MockSendAsync(statusCode: HttpStatusCode.Forbidden);

            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithUrl("https://cdn.optimizely.com/datafiles/QBw9gFM8oTn7ogY9ANCC1z.json")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .WithStartByDefault()
                .Build();

            LoggerMock.Verify(_ => _.Log(LogLevel.ERROR, $"Error fetching datafile \"{HttpStatusCode.Forbidden}\""), Times.AtLeastOnce);

            httpManager.Dispose();
        }

        [Test]
        public void TestHttpClientHandler()
        {
            var httpConfigHandler = HttpProjectConfigManager.HttpClient.GetHttpClientHandler();
            Assert.IsTrue(httpConfigHandler.AutomaticDecompression == (System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip));
        }

        [Test]
        public void TestHttpConfigManagerRetreiveProjectConfigGivenEmptyFormatUseDefaultFormat()
        {
            var t = MockSendAsync(TestData.Datafile);

            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                 .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                 .WithFormat("")
                 .WithLogger(LoggerMock.Object)
                 .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                 .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                 .WithStartByDefault()
                 .Build();

            // This "Wait" notifies When SendAsync is triggered.
            // Time is given here to avoid hanging-up in any worst case.
            t.Wait(1000);
            HttpClientMock.Verify(_ => _.SendAsync(
                It.Is<System.Net.Http.HttpRequestMessage>(requestMessage =>
                requestMessage.RequestUri.ToString() == "https://cdn.optimizely.com/datafiles/QBw9gFM8oTn7ogY9ANCC1z.json"
                )));
            httpManager.Dispose();
        }

        [Test]
        public void TestHttpConfigManagerRetreiveProjectConfigBySDKKey()
        {
            var t = MockSendAsync(TestData.Datafile);

            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .WithStartByDefault()
                .Build();

            t.Wait(1000);
            HttpClientMock.Verify(_ => _.SendAsync(
                It.Is<System.Net.Http.HttpRequestMessage>(requestMessage =>
                requestMessage.RequestUri.ToString() == "https://cdn.optimizely.com/datafiles/QBw9gFM8oTn7ogY9ANCC1z.json"
                )));
            Assert.IsNotNull(httpManager.GetConfig());
            httpManager.Dispose();
        }

        [Test]
        public void TestHttpConfigManagerRetreiveProjectConfigByFormat()
        {
            var t = MockSendAsync(TestData.Datafile);

            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("10192104166")
                .WithFormat("https://cdn.optimizely.com/json/{0}.json")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .WithStartByDefault()
                .Build(true);

            t.Wait(1000);
            HttpClientMock.Verify(_ => _.SendAsync(
                It.Is<System.Net.Http.HttpRequestMessage>(requestMessage =>
                requestMessage.RequestUri.ToString() == "https://cdn.optimizely.com/json/10192104166.json"
                )));
            Assert.IsNotNull(httpManager.GetConfig());
            LoggerMock.Verify(_ => _.Log(LogLevel.DEBUG, "Making datafile request to url \"https://cdn.optimizely.com/json/10192104166.json\""));
            httpManager.Dispose();
        }

        [Test]
        public void TestOnReadyPromiseResolvedImmediatelyWhenDatafileIsProvided()
        {
            // Revision - 42
            var t = MockSendAsync(TestData.SimpleABExperimentsDatafile, TimeSpan.FromMilliseconds(100));

            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                // Revision - 15
                .WithSdkKey("10192104166")
                .WithDatafile(TestData.Datafile)
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .WithStartByDefault()
                .Build();

            // OnReady waits until is resolved, need to add time in case of deadlock.
            httpManager.OnReady().Wait(10000);

            Assert.AreEqual("15", httpManager.GetConfig().Revision);

            // loaded datafile from config manager after a second.
            // This wait triggers when SendAsync is triggered, OnReadyPromise is already resolved because of hardcoded datafile.
            t.Wait();
            Task.Delay(200).Wait();
            Assert.AreEqual("42", httpManager.GetConfig().Revision);
            httpManager.Dispose();
        }

        [Test]
        public void TestOnReadyPromiseWaitsForProjectConfigRetrievalWhenDatafileIsNotProvided()
        {
            // Revision - 42
            var t = MockSendAsync(TestData.SimpleABExperimentsDatafile, TimeSpan.FromMilliseconds(1000));

            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromSeconds(2))
                .WithBlockingTimeoutPeriod(TimeSpan.FromSeconds(1))
                .WithStartByDefault(true)
                .Build();
            t.Wait();

            // OnReady waits until is resolved, need to add time in case of deadlock.
            httpManager.OnReady().Wait(10000);
            Assert.NotNull(httpManager.GetConfig());
            httpManager.Dispose();
        }

        [Test]
        public void TestHttpConfigManagerDoesNotWaitForTheConfigWhenDeferIsTrue()
        {
            var t = MockSendAsync(TestData.Datafile, TimeSpan.FromMilliseconds(150));

            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromSeconds(2))
                // negligible timeout
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(50))
                .WithStartByDefault()
                .Build(false);

            // When blocking timeout is 0 and defer is false and getconfig immediately called
            // should return null
            Assert.IsNull(httpManager.GetConfig());
            // wait until config is retrieved.
            t.Wait();
            // in case deadlock, it will release after 3sec.
            httpManager.OnReady().Wait(8000);
                
            HttpClientMock.Verify(_ => _.SendAsync(It.IsAny<HttpRequestMessage>()));
            Assert.NotNull(httpManager.GetConfig());

            httpManager.Dispose();
        }

        #region Notification
        [Test]
        public void TestHttpConfigManagerSendConfigUpdateNotificationWhenProjectConfigGetsUpdated()
        {            
            var t = MockSendAsync(TestData.Datafile);

            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(1000))
                .WithStartByDefault(false)
                .Build(true);

            httpManager.NotifyOnProjectConfigUpdate += NotificationCallbackMock.Object.TestConfigUpdateCallback;
            httpManager.Start();

            Assert.NotNull(httpManager.GetConfig());
            Task.Delay(200).Wait();
            NotificationCallbackMock.Verify(nc => nc.TestConfigUpdateCallback(), Times.AtLeastOnce);
            httpManager.Dispose();
        }

        [Test]
        public void TestHttpConfigManagerDoesNotSendConfigUpdateNotificationWhenDatafileIsProvided()
        {            
            var t = MockSendAsync(TestData.Datafile, TimeSpan.FromMilliseconds(100));

            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithDatafile(TestData.Datafile)
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .Build();


            httpManager.NotifyOnProjectConfigUpdate += NotificationCallbackMock.Object.TestConfigUpdateCallback;

            NotificationCallbackMock.Verify(nc => nc.TestConfigUpdateCallback(), Times.Never);
            Assert.NotNull(httpManager.GetConfig()); Assert.NotNull(httpManager.GetConfig());
            httpManager.Dispose();
        }

        [Test]
        public void TestDefaultBlockingTimeoutWhileProvidingZero()
        {
            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithDatafile(TestData.Datafile)
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(0))
                .WithStartByDefault(true)
                .Build(true);

            var fieldInfo = httpManager.GetType().GetField("BlockingTimeout", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var expectedBlockingTimeout = (TimeSpan)fieldInfo.GetValue(httpManager);
            Assert.AreNotEqual(expectedBlockingTimeout.TotalSeconds, TimeSpan.Zero.TotalSeconds);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, $"Blocking timeout is not valid, using default blocking timeout {TimeSpan.FromSeconds(15).TotalMilliseconds}ms"));
            httpManager.Dispose();
        }

        [Test]
        public void TestDefaultPeriodWhileProvidingZero()
        {
            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithDatafile(TestData.Datafile)
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(0))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(1000))
                .WithStartByDefault(true)
                .Build(true);

            var fieldInfo = typeof(PollingProjectConfigManager).GetField("PollingInterval", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var expectedPollingInterval = (TimeSpan)fieldInfo.GetValue(httpManager);
            Assert.AreNotEqual(expectedPollingInterval.TotalSeconds, TimeSpan.Zero.TotalSeconds);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, $"Polling interval is not valid for periodic calls, using default period {TimeSpan.FromMinutes(5).TotalMilliseconds}ms"));
            httpManager.Dispose();
        }

        [Test]
        public void TestDefaultPeriodWhileProvidingNegative()
        {
            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithDatafile(TestData.Datafile)
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(-1))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(1000))
                .WithStartByDefault(true)
                .Build(true);

            var fieldInfo = typeof(PollingProjectConfigManager).GetField("PollingInterval", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var expectedPollingInterval = (TimeSpan)fieldInfo.GetValue(httpManager);
            Assert.AreNotEqual(expectedPollingInterval.TotalSeconds, TimeSpan.Zero.TotalSeconds);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, $"Polling interval is not valid for periodic calls, using default period {TimeSpan.FromMinutes(5).TotalMilliseconds}ms"));
            httpManager.Dispose();
        }

        [Test]
        public void TestDefaultPeriodWhileNotProvidingValue()
        {
            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithDatafile(TestData.Datafile)
                .WithLogger(LoggerMock.Object)
                .Build(true);

            var fieldInfo = typeof(PollingProjectConfigManager).GetField("PollingInterval", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var expectedPollingInterval = (TimeSpan)fieldInfo.GetValue(httpManager);
            Assert.AreNotEqual(expectedPollingInterval.TotalSeconds, TimeSpan.Zero.TotalSeconds);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, $"No polling interval provided, using default period {TimeSpan.FromMinutes(5).TotalMilliseconds}ms"));
            httpManager.Dispose();
        }

        [Test]
        public void TestDefaultBlockingTimeoutWhileNotProvidingValue()
        {
            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithDatafile(TestData.Datafile)
                .WithLogger(LoggerMock.Object)
                .Build(true);

            var fieldInfo = httpManager.GetType().GetField("BlockingTimeout", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var expectedBlockingTimeout = (TimeSpan)fieldInfo.GetValue(httpManager);
            Assert.AreNotEqual(expectedBlockingTimeout.TotalSeconds, TimeSpan.Zero.TotalSeconds);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, $"No Blocking timeout provided, using default blocking timeout {TimeSpan.FromSeconds(15).TotalMilliseconds}ms"));
            httpManager.Dispose();
        }

        [Test]
        public void TestDefaultValuesWhenNotProvided()
        {
            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithDatafile(TestData.Datafile)
                .WithLogger(LoggerMock.Object)
                .Build(true);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, $"No polling interval provided, using default period {TimeSpan.FromMinutes(5).TotalMilliseconds}ms"));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, $"No Blocking timeout provided, using default blocking timeout {TimeSpan.FromSeconds(15).TotalMilliseconds}ms"));
            httpManager.Dispose();
        }

        [Test]
        public void TestAuthUrlWhenTokenProvided()
        {
            var t = MockSendAsync();

            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .WithAccessToken("datafile1")
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(50))
                .Build(true);

            // it's to wait if SendAsync is not triggered.
            t.Wait(2000);

            HttpClientMock.Verify(_ => _.SendAsync(
                It.Is<System.Net.Http.HttpRequestMessage>(requestMessage =>
                requestMessage.RequestUri.ToString() == "https://config.optimizely.com/datafiles/auth/QBw9gFM8oTn7ogY9ANCC1z.json"
                )));
            httpManager.Dispose();
        }

        [Test]
        public void TestDefaultUrlWhenTokenNotProvided()
        {
            var t = MockSendAsync();

            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(50))
                .Build(true);

            // it's to wait if SendAsync is not triggered.
            t.Wait(2000);
            HttpClientMock.Verify(_ => _.SendAsync(
                It.Is<System.Net.Http.HttpRequestMessage>(requestMessage =>
                requestMessage.RequestUri.ToString() == "https://cdn.optimizely.com/datafiles/QBw9gFM8oTn7ogY9ANCC1z.json"
                )));
            httpManager.Dispose();
        }

        [Test]
        public void TestAuthenticationHeaderWhenTokenProvided()
        {
            var t = MockSendAsync();

            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(50))
                .WithAccessToken("datafile1")
                .Build(true);

            // it's to wait if SendAsync is not triggered.
            t.Wait(2000);

            HttpClientMock.Verify(_ => _.SendAsync(
                It.Is<System.Net.Http.HttpRequestMessage>(requestMessage =>
                requestMessage.Headers.Authorization.ToString() == "Bearer datafile1"
                )));
            httpManager.Dispose();
        }

        [Test]
        public void TestFormatUrlHigherPriorityThanDefaultUrl()
        {
            var t = MockSendAsync();
            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .WithFormat("http://customformat/{0}.json")
                .WithAccessToken("datafile1")
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(50))
                .Build(true);
            // it's to wait if SendAsync is not triggered.
            t.Wait(2000);
            HttpClientMock.Verify(_ => _.SendAsync(
                It.Is<System.Net.Http.HttpRequestMessage>(requestMessage =>
                requestMessage.RequestUri.ToString() == "http://customformat/QBw9gFM8oTn7ogY9ANCC1z.json"
                )));
            httpManager.Dispose();

        }

        public Task MockSendAsync(string datafile = null, TimeSpan? delay = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return TestHttpProjectConfigManagerUtil.MockSendAsync(HttpClientMock, datafile, delay, statusCode);
        }
        #endregion
    }
}
