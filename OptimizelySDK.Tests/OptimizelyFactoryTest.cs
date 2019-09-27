/* 
 * Copyright 2019, Optimizely
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
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;
using OptimizelySDK.Tests.NotificationTests;
using System;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    class OptimizelyFactoryTest
    {
        private Mock<ILogger> LoggerMock;
        private ProjectConfigManager ConfigManager;
        private ProjectConfig Config;
        private Mock<IErrorHandler> ErrorHandlerMock;
        private Mock<IEventDispatcher> EventDispatcherMock;
        private Mock<TestNotificationCallbacks> NotificationCallbackMock;


        [SetUp]
        public void Initialize()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            ErrorHandlerMock = new Mock<IErrorHandler>();
            ErrorHandlerMock.Setup(e => e.HandleError(It.IsAny<Exception>()));

            var config = DatafileProjectConfig.Create(
                content: TestData.Datafile,
                logger: LoggerMock.Object,
                errorHandler: new NoOpErrorHandler());
            EventDispatcherMock = new Mock<IEventDispatcher>();

            NotificationCallbackMock = new Mock<TestNotificationCallbacks>();
        }

        [TestFixtureTearDown]
        public void Cleanup()
        {
            LoggerMock = null;
            Config = null;
        }

        [Test]
        public void TestOptimizelyFactoryWithValidSdkKey()
        {
            var optimizely = OptimizelyFactory.NewDefaultInstance("QBw9gFM8oTn7ogY9ANCC1z");

            Assert.NotNull(optimizely.ProjectConfigManager.GetConfig());
            Assert.IsTrue(optimizely.IsValid);
        }


        [Test]
        public void TestOptimizelyFactoryWithInvalidSdkKey()
        {
            var optimizely = OptimizelyFactory.NewDefaultInstance("invalid");

            Assert.Null(optimizely.ProjectConfigManager.GetConfig());
            Assert.IsFalse(optimizely.IsValid);
        }

        [Test]
        public void TestOptimizelyFactoryWithInvalidSdkKeyAndValidFallBack()
        {
            var optimizely = OptimizelyFactory.NewDefaultInstance("invalid", TestData.Datafile);

            Assert.NotNull(optimizely.ProjectConfigManager.GetConfig());
            Assert.IsTrue(optimizely.IsValid);
            optimizely.Dispose();
        }

        [Test]
        public void TestWithValidProjectConfigManagerAndNotificationCenter()
        {
            NotificationCenter notificationCenter = new NotificationCenter();
            NotificationCallbackMock.Setup(notification => notification.TestConfigUpdateCallback());


            var httpManager = new HttpProjectConfigManager.Builder()
                                                          .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                                                          .WithLogger(LoggerMock.Object)
                                                          .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                                                          .WithStartByDefault(false)
                                                          .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                                                          .WithNotificationCenter(notificationCenter)
                                                          .Build();

            var optimizely = OptimizelyFactory.NewDefaultInstance(httpManager, notificationCenter);
            httpManager.Start();
            optimizely.NotificationCenter.AddNotification(NotificationCenter.NotificationType.OptimizelyConfigUpdate, NotificationCallbackMock.Object.TestConfigUpdateCallback);
            httpManager.OnReady().Wait(-1);

            NotificationCallbackMock.Verify(nc => nc.TestConfigUpdateCallback(), Times.Once);
            optimizely.Dispose();
        }

        [Test]
        public void TestOptimizelyFactorySettingVariables()
        {
            OptimizelyFactory.SetBatchSize(22);
            OptimizelyFactory.SetBlockingTimeoutPeriod(TimeSpan.FromSeconds(10));
            OptimizelyFactory.SetFlushInterval(TimeSpan.FromMilliseconds(1200));
            OptimizelyFactory.SetPollingInterval(TimeSpan.FromMinutes(1));

            var optimizely = OptimizelyFactory.NewDefaultInstance("QBw9gFM8oTn7ogY9ANCC1z");
            Assert.IsNotNull(optimizely.ProjectConfigManager.GetConfig());
            Assert.IsTrue(optimizely.IsValid);
            optimizely.Dispose();
        }

        [Test]
        public void TestOptimizelyFactoryGettingVariablesFromAppConfig()
        {
            var optimizely = OptimizelyFactory.NewDefaultInstance();
            Assert.IsNotNull(optimizely.ProjectConfigManager.GetConfig());
            Assert.IsTrue(optimizely.IsValid);
            optimizely.Dispose();
        }
    }
}

