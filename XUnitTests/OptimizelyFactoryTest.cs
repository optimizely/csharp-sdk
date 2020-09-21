/**
 *
 *    Copyright 2020, Optimizely and contributors
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

using Moq;
using Xunit;
using OptimizelySDK.Config;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;
using OptimizelySDK.XUnitTests.ConfigTest;
using OptimizelySDK.XUnitTests.EventTest;
using OptimizelySDK.XUnitTests.Utils;
using System;
namespace OptimizelySDK.XUnitTests
{
    public class OptimizelyFactoryTest
    {
        private Mock<ILogger> LoggerMock;
        public OptimizelyFactoryTest()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
        }

        // TODO remove the comment below and resolve the issue 

        //[Fact]
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
                PollingInterval = TimeSpan.FromSeconds(2)
            };

            Assert.Equal(actualConfigManagerProps, expectedConfigManagerProps);
            optimizely.Dispose();
        }


        [Fact]
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
                BlockingTimeout = TimeSpan.FromSeconds(15),
                PollingInterval = TimeSpan.FromMinutes(5)
            };

            Assert.Equal(actualConfigManagerProps, expectedConfigManagerProps);
            optimizely.Dispose();
        }

        [Fact]
        public void TestProjectConfigManagerWithDatafileAccessToken()
        {
            var optimizely = OptimizelyFactory.NewDefaultInstance("my-sdk-key", null, "access-token");

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
                BlockingTimeout = TimeSpan.FromSeconds(15),
                PollingInterval = TimeSpan.FromMinutes(5)
            };

            Assert.Equal(actualConfigManagerProps, expectedConfigManagerProps);

            optimizely.Dispose();
        }

        [Fact]
        public void TestProjectConfigManagerWithCustomProjectConfigManager()
        {
            var projectConfigManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("10192104166")
                .WithFormat("https://optimizely.com/json/{0}.json")
                .WithPollingInterval(TimeSpan.FromMilliseconds(3000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(4500))
                .WithStartByDefault()
                .WithAccessToken("access-token")
                .Build(true);

            var optimizely = OptimizelyFactory.NewDefaultInstance(projectConfigManager);
            var actualProjectConfigManager = optimizely.ProjectConfigManager as HttpProjectConfigManager;
            var actualConfigManagerProps = new ProjectConfigManagerProps(actualProjectConfigManager);
            var expectedConfigManagerProps = new ProjectConfigManagerProps(projectConfigManager);
            Assert.Equal(actualConfigManagerProps, expectedConfigManagerProps);
            optimizely.Dispose();
        }

        // TODO remove the comment below and resolve the issue 
        //[Fact]
        public void TestEventProcessorWithDefaultEventBatching()
        {
            var optimizely = OptimizelyFactory.NewDefaultInstance();

            var batchEventProcessor = Reflection.GetFieldValue<BatchEventProcessor, Optimizely>(optimizely, "EventProcessor");
            var actualEventProcessorProps = new EventProcessorProps(batchEventProcessor);
            var expectedEventProcessorProps = new EventProcessorProps
            {
                BatchSize = 10,
                FlushInterval = TimeSpan.FromSeconds(2),
                TimeoutInterval = TimeSpan.FromSeconds(10)
            };
            Assert.Equal(actualEventProcessorProps, expectedEventProcessorProps);
            optimizely.Dispose();
        }

        [Fact]
        public void TestEventProcessorWithEventBatchingBatchSizeAndInterval()
        {
            OptimizelyFactory.SetBatchSize(2);
            OptimizelyFactory.SetFlushInterval(TimeSpan.FromSeconds(4));

            var optimizely = OptimizelyFactory.NewDefaultInstance("sdk-Key");

            var batchEventProcessor = Reflection.GetFieldValue<BatchEventProcessor, Optimizely>(optimizely, "EventProcessor");
            var actualEventProcessorProps = new EventProcessorProps(batchEventProcessor);
            var expectedEventProcessorProps = new EventProcessorProps
            {
                BatchSize = 2,
                FlushInterval = TimeSpan.FromSeconds(4),
                TimeoutInterval = TimeSpan.FromMinutes(5)
            };
            Assert.Equal(actualEventProcessorProps, expectedEventProcessorProps);
            optimizely.Dispose();
        }

        [Fact]
        public void TestEventProcessorWithBatchEventProcessorObj()
        {
            var eventDispatcher = new DefaultEventDispatcher(LoggerMock.Object);
            var notificationCenter = new NotificationCenter();
            var projectConfigManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("10192104166")
                .Build(true);

            var batchEventProcessor = new BatchEventProcessor.Builder()
                .WithLogger(LoggerMock.Object)
                .WithMaxBatchSize(20)
                .WithFlushInterval(TimeSpan.FromSeconds(3))
                .WithEventDispatcher(eventDispatcher)
                .WithNotificationCenter(notificationCenter)
                .Build();

            var optimizely = OptimizelyFactory.NewDefaultInstance(projectConfigManager, notificationCenter, eventProcessor: batchEventProcessor);

            var actualbatchEventProcessor = Reflection.GetFieldValue<BatchEventProcessor, Optimizely>(optimizely, "EventProcessor");
            var actualEventProcessorProps = new EventProcessorProps(actualbatchEventProcessor);
            var expectedEventProcessorProps = new EventProcessorProps(batchEventProcessor);
            Assert.Equal(actualEventProcessorProps, expectedEventProcessorProps);
            optimizely.Dispose();
        }
    }

}
