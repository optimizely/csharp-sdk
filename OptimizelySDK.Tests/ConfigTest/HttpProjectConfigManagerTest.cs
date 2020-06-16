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
using System;
using System.Diagnostics;
using System.Net.Http;

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
            HttpClientMock = new Mock<HttpProjectConfigManager.HttpClient> { CallBase = true };
            HttpClientMock.Reset();
            var field = typeof(HttpProjectConfigManager).GetField("Client",
                            System.Reflection.BindingFlags.Static |
                            System.Reflection.BindingFlags.NonPublic);
            field.SetValue(field, HttpClientMock.Object);
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            NotificationCallbackMock.Setup(nc => nc.TestConfigUpdateCallback());

        }

        [Test]
        public void TestHttpConfigManagerRetreiveProjectConfigByURL()
        {
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithUrl("https://cdn.optimizely.com/datafiles/QBw9gFM8oTn7ogY9ANCC1z.json")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .WithStartByDefault()
                .Build();

            httpManager.OnReady().Wait(System.Threading.Timeout.Infinite);
            Assert.NotNull(httpManager.GetConfig());
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
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                 .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                 .WithFormat("")
                 .WithLogger(LoggerMock.Object)
                 .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                 .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                 .WithStartByDefault()
                 .Build();

            httpManager.OnReady().Wait(System.Threading.Timeout.Infinite);
            Assert.NotNull(httpManager.GetConfig());
        }

        [Test]
        public void TestHttpConfigManagerRetreiveProjectConfigBySDKKey()
        {
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .WithStartByDefault()
                .Build();

            httpManager.OnReady().Wait(System.Threading.Timeout.Infinite);
            Assert.NotNull(httpManager.GetConfig());
        }

        [Test]
        public void TestHttpConfigManagerRetreiveProjectConfigByFormat()
        {
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("10192104166")
                .WithFormat("https://cdn.optimizely.com/json/{0}.json")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .WithStartByDefault()
                .Build(true);

            httpManager.OnReady().Wait(System.Threading.Timeout.Infinite);
            Assert.NotNull(httpManager.GetConfig());
        }

        [Test]
        public void TestOnReadyPromiseResolvedImmediatelyWhenDatafileIsProvided()
        {
            var stopwatch = new Stopwatch();
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("10192104166")
                .WithDatafile(TestData.Datafile)
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .WithStartByDefault()
                .Build();

            stopwatch.Start();
            httpManager.OnReady().Wait();
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed.Seconds == 0);
            Assert.NotNull(httpManager.GetConfig());
        }

        [Test]
        public void TestOnReadyPromiseWaitsForProjectConfigRetrievalWhenDatafileIsNotProvided()
        {
            var stopwatch = new Stopwatch();
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromSeconds(2))
                .WithBlockingTimeoutPeriod(TimeSpan.FromSeconds(1))
                .WithStartByDefault(true)
                .Build();

            httpManager.OnReady().Wait();
            Assert.NotNull(httpManager.GetConfig());
        }

        [Test]
        public void TestHttpConfigManagerDoesNotWaitForTheConfigWhenDeferIsTrue()
        {
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
        }

        #region Notification
        [Test]
        public void TestHttpConfigManagerSendConfigUpdateNotificationWhenProjectConfigGetsUpdated()
        {
            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))                
                .WithStartByDefault(true)
                .Build();
        
            httpManager.OnReady().Wait(System.Threading.Timeout.Infinite);
            httpManager.NotifyOnProjectConfigUpdate += NotificationCallbackMock.Object.TestConfigUpdateCallback;

            NotificationCallbackMock.Verify(nc => nc.TestConfigUpdateCallback(), Times.AtLeastOnce);
            Assert.NotNull(httpManager.GetConfig());
        }

        [Test]
        public void TestHttpConfigManagerDoesNotSendConfigUpdateNotificationWhenDatafileIsProvided()
        {
            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithDatafile(TestData.Datafile)
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))                
                .WithStartByDefault(true)
                .Build();


            httpManager.NotifyOnProjectConfigUpdate += NotificationCallbackMock.Object.TestConfigUpdateCallback;

            NotificationCallbackMock.Verify(nc => nc.TestConfigUpdateCallback(), Times.Never);
            Assert.NotNull(httpManager.GetConfig()); Assert.NotNull(httpManager.GetConfig());
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

        }

        public System.Threading.Tasks.Task MockSendAsync()
        {
            var t = new System.Threading.Tasks.TaskCompletionSource<bool>();

            HttpClientMock.Setup(_ => _.SendAsync(It.IsAny<System.Net.Http.HttpRequestMessage>()))
                .Returns(System.Threading.Tasks.Task.FromResult<HttpResponseMessage>(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK, Content = new StringContent(string.Empty) }))
                .Callback(()
                => {
                    t.SetResult(true);
                });

            return t.Task;
        }

        #endregion
    }
}
