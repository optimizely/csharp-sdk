/**
 *
 *    Copyright 2020-2021, Optimizely and contributors
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */

using System;
using System.Reflection;
using Moq;
using NUnit.Framework;
using OptimizelySDK.Bucketing;
using OptimizelySDK.Cmab;
using OptimizelySDK.Config;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;
using OptimizelySDK.Odp;
using OptimizelySDK.Tests.ConfigTest;
using OptimizelySDK.Utils;
using OptimizelySDK.Tests.EventTest;
using OptimizelySDK.Tests.Utils;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class OptimizelyFactoryTest
    {
        private Mock<ILogger> LoggerMock;

        [SetUp]
        public void Initialize()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            ResetCmabConfiguration();
        }

        [TearDown]
        public void Cleanup()
        {
            ResetCmabConfiguration();
        }

        [Test]
        public void TestOptimizelyInstanceUsingConfigFile()
        {
            var optimizely = OptimizelyFactory.NewDefaultInstance();
            // Check values are loaded from app.config or not.
            var projectConfigManager = optimizely.ProjectConfigManager as HttpProjectConfigManager;
            Assert.NotNull(projectConfigManager);

            var actualConfigManagerProps = new ProjectConfigManagerProps(projectConfigManager);
            var expectedConfigManagerProps = new ProjectConfigManagerProps
            {
                Url = "www.testurl.com",
                LastModified = "",
                AutoUpdate = true,
                DatafileAccessToken = "testingtoken123",
                BlockingTimeout = TimeSpan.FromSeconds(10),
                PollingInterval = TimeSpan.FromSeconds(2),
            };

            Assert.AreEqual(actualConfigManagerProps, expectedConfigManagerProps);
            optimizely.Dispose();
        }

        [Test]
        public void TestProjectConfigManagerUsingSDKKey()
        {
            var optimizely = OptimizelyFactory.NewDefaultInstance("my-sdk-key");

            // Check values are loaded from app.config or not.
            var projectConfigManager = optimizely.ProjectConfigManager as HttpProjectConfigManager;
            Assert.NotNull(projectConfigManager);

            var actualConfigManagerProps = new ProjectConfigManagerProps(projectConfigManager);
            var expectedConfigManagerProps = new ProjectConfigManagerProps
            {
                Url = "https://cdn.optimizely.com/datafiles/my-sdk-key.json",
                LastModified = "",
                AutoUpdate = true,
                BlockingTimeout = TimeSpan.FromSeconds(30),
                PollingInterval = TimeSpan.FromMilliseconds(2023),
            };

            Assert.AreEqual(actualConfigManagerProps, expectedConfigManagerProps);
            optimizely.Dispose();
        }

        [Test]
        public void TestProjectConfigManagerWithDatafileAccessToken()
        {
            var optimizely =
                OptimizelyFactory.NewDefaultInstance("my-sdk-key", null, "access-token");

            // Check values are loaded from app.config or not.
            var projectConfigManager = optimizely.ProjectConfigManager as HttpProjectConfigManager;
            Assert.NotNull(projectConfigManager);

            var actualConfigManagerProps = new ProjectConfigManagerProps(projectConfigManager);
            var expectedConfigManagerProps = new ProjectConfigManagerProps
            {
                Url = "https://config.optimizely.com/datafiles/auth/my-sdk-key.json",
                LastModified = "",
                DatafileAccessToken = "access-token",
                AutoUpdate = true,
                BlockingTimeout = TimeSpan.FromSeconds(30),
                PollingInterval = TimeSpan.FromMilliseconds(2023),
            };

            Assert.AreEqual(actualConfigManagerProps, expectedConfigManagerProps);

            optimizely.Dispose();
        }

        [Test]
        public void
            TestOptimizelyInstanceUsingConfigNotUseFactoryClassBlockingTimeoutAndPollingInterval()
        {
            OptimizelyFactory.SetBlockingTimeOutPeriod(TimeSpan.FromSeconds(30));
            OptimizelyFactory.SetPollingInterval(TimeSpan.FromMilliseconds(2023));
            var optimizely = OptimizelyFactory.NewDefaultInstance();
            // Check values are loaded from app.config or not.
            var projectConfigManager = optimizely.ProjectConfigManager as HttpProjectConfigManager;
            Assert.NotNull(projectConfigManager);

            var actualConfigManagerProps = new ProjectConfigManagerProps(projectConfigManager);
            var expectedConfigManagerProps = new ProjectConfigManagerProps
            {
                Url = "www.testurl.com",
                LastModified = "",
                AutoUpdate = true,
                DatafileAccessToken = "testingtoken123",
                BlockingTimeout = TimeSpan.FromMilliseconds(10000),
                PollingInterval = TimeSpan.FromMilliseconds(2000),
            };

            Assert.AreEqual(actualConfigManagerProps, expectedConfigManagerProps);
            optimizely.Dispose();
        }

        [Test]
        public void TestProjectConfigManagerWithCustomProjectConfigManager()
        {
            var projectConfigManager = new HttpProjectConfigManager.Builder().
                WithSdkKey("10192104166").
                WithFormat("https://optimizely.com/json/{0}.json").
                WithPollingInterval(TimeSpan.FromMilliseconds(3000)).
                WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(4500)).
                WithStartByDefault().
                WithAccessToken("access-token").
                Build(true);

            var optimizely = OptimizelyFactory.NewDefaultInstance(projectConfigManager);
            var actualProjectConfigManager =
                optimizely.ProjectConfigManager as HttpProjectConfigManager;
            var actualConfigManagerProps =
                new ProjectConfigManagerProps(actualProjectConfigManager);
            var expectedConfigManagerProps = new ProjectConfigManagerProps(projectConfigManager);
            Assert.AreEqual(actualConfigManagerProps, expectedConfigManagerProps);
            optimizely.Dispose();
        }

        [Test]
        public void TestEventProcessorWithDefaultEventBatching()
        {
            var optimizely = OptimizelyFactory.NewDefaultInstance();

            var batchEventProcessor =
                Reflection.GetFieldValue<BatchEventProcessor, Optimizely>(optimizely,
                    "EventProcessor");
            var actualEventProcessorProps = new EventProcessorProps(batchEventProcessor);
            var expectedEventProcessorProps = new EventProcessorProps
            {
                BatchSize = 10,
                FlushInterval = TimeSpan.FromSeconds(2),
                TimeoutInterval = TimeSpan.FromSeconds(10),
            };
            Assert.AreEqual(actualEventProcessorProps, expectedEventProcessorProps);
            optimizely.Dispose();
        }

        [Test]
        public void TestEventProcessorWithEventBatchingBatchSizeAndInterval()
        {
            OptimizelyFactory.SetBatchSize(2);
            OptimizelyFactory.SetFlushInterval(TimeSpan.FromSeconds(4));

            var optimizely = OptimizelyFactory.NewDefaultInstance("sdk-Key");

            var batchEventProcessor =
                Reflection.GetFieldValue<BatchEventProcessor, Optimizely>(optimizely,
                    "EventProcessor");
            var actualEventProcessorProps = new EventProcessorProps(batchEventProcessor);
            var expectedEventProcessorProps = new EventProcessorProps
            {
                BatchSize = 2,
                FlushInterval = TimeSpan.FromSeconds(4),
                TimeoutInterval = TimeSpan.FromMinutes(5),
            };
            Assert.AreEqual(actualEventProcessorProps, expectedEventProcessorProps);
            optimizely.Dispose();
        }

        [Test]
        public void TestEventProcessorWithBatchEventProcessorObj()
        {
            var eventDispatcher = new DefaultEventDispatcher(LoggerMock.Object);
            var notificationCenter = new NotificationCenter();
            var projectConfigManager = new HttpProjectConfigManager.Builder().
                WithSdkKey("10192104166").
                Build(true);

            var batchEventProcessor = new BatchEventProcessor.Builder().
                WithLogger(LoggerMock.Object).
                WithMaxBatchSize(20).
                WithFlushInterval(TimeSpan.FromSeconds(3)).
                WithEventDispatcher(eventDispatcher).
                WithNotificationCenter(notificationCenter).
                Build();

            var optimizely = OptimizelyFactory.NewDefaultInstance(projectConfigManager,
                notificationCenter, eventProcessor: batchEventProcessor);

            var actualbatchEventProcessor =
                Reflection.GetFieldValue<BatchEventProcessor, Optimizely>(optimizely,
                    "EventProcessor");
            var actualEventProcessorProps = new EventProcessorProps(actualbatchEventProcessor);
            var expectedEventProcessorProps = new EventProcessorProps(batchEventProcessor);
            Assert.AreEqual(actualEventProcessorProps, expectedEventProcessorProps);
            optimizely.Dispose();
        }

        [Test]
        public void TestGetFeatureVariableJSONEmptyDatafileTest()
        {
            var httpClientMock = new Mock<HttpProjectConfigManager.HttpClient>();
            var task = TestHttpProjectConfigManagerUtil.MockSendAsync(httpClientMock,
                TestData.EmptyDatafile, TimeSpan.Zero, System.Net.HttpStatusCode.OK);
            TestHttpProjectConfigManagerUtil.SetClientFieldValue(httpClientMock.Object);

            var optimizely = OptimizelyFactory.NewDefaultInstance("sdk-key");
            Assert.Null(optimizely.GetFeatureVariableJSON("no-feature-variable", "no-variable-key",
                "userId"));
            optimizely.Dispose();
        }

        [Test]
        public void SetCmabCacheConfigStoresCacheSizeAndTtl()
        {
            const int cacheSize = 1234;
            var cacheTtl = TimeSpan.FromSeconds(45);

            OptimizelyFactory.SetCmabCacheConfig(cacheSize, cacheTtl);

            var config = GetCurrentCmabConfiguration();

            Assert.IsNotNull(config);
            Assert.AreEqual(cacheSize, config.CacheSize);
            Assert.AreEqual(cacheTtl, config.CacheTtl);
            Assert.IsNull(config.CustomCache);
        }

        [Test]
        public void SetCmabCustomCacheStoresCustomCacheInstance()
        {
            var customCache = new LruCache<CmabCacheEntry>(maxSize: 10, itemTimeout: TimeSpan.FromMinutes(2));

            OptimizelyFactory.SetCmabCustomCache(customCache);

            var config = GetCurrentCmabConfiguration();

            Assert.IsNotNull(config);
            Assert.AreSame(customCache, config.CustomCache);
            Assert.IsNull(config.CacheSize);
            Assert.IsNull(config.CacheTtl);
        }

        [Test]
        public void NewDefaultInstanceUsesConfiguredCmabCache()
        {
            const int cacheSize = 7;
            var cacheTtl = TimeSpan.FromSeconds(30);
            OptimizelyFactory.SetCmabCacheConfig(cacheSize, cacheTtl);

            var logger = new NoOpLogger();
            var errorHandler = new NoOpErrorHandler();
            var projectConfig = DatafileProjectConfig.Create(TestData.Datafile, logger, errorHandler);
            var configManager = new FallbackProjectConfigManager(projectConfig);

            var optimizely = OptimizelyFactory.NewDefaultInstance(configManager, logger: logger, errorHandler: errorHandler);

            var decisionService = Reflection.GetFieldValue<DecisionService, Optimizely>(optimizely, "DecisionService");
            Assert.IsNotNull(decisionService);

            var cmabService = Reflection.GetFieldValue<ICmabService, DecisionService>(decisionService, "CmabService");
            Assert.IsInstanceOf<DefaultCmabService>(cmabService);

            var cache = Reflection.GetFieldValue<ICacheWithRemove<CmabCacheEntry>, DefaultCmabService>((DefaultCmabService)cmabService, "_cmabCache") as LruCache<CmabCacheEntry>;
            Assert.IsNotNull(cache);
            Assert.AreEqual(cacheSize, cache.MaxSizeForTesting);
            Assert.AreEqual(cacheTtl, cache.TimeoutForTesting);

            optimizely.Dispose();
        }

        private static void ResetCmabConfiguration()
        {
            var field = typeof(OptimizelyFactory).GetField("CmabConfiguration", BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, null);
        }

        private static CmabConfig GetCurrentCmabConfiguration()
        {
            var field = typeof(OptimizelyFactory).GetField("CmabConfiguration", BindingFlags.NonPublic | BindingFlags.Static);
            return field?.GetValue(null) as CmabConfig;
        }
    }
}
