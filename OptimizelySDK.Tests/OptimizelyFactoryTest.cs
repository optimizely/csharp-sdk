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

using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.Event;
using OptimizelySDK.Tests.ConfigTest;
using OptimizelySDK.Tests.EventTest;
using OptimizelySDK.Tests.Utils;
using System;
namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class OptimizelyFactoryTest
    {
        [Test]
        public void TestOptimizelyInstanceUsingConfigFile()
        {
            var optimizely = OptimizelyFactory.NewDefaultInstance();
            // Check values are loaded from app.config or not.
            var projectConfigManager = optimizely.ProjectConfigManager as HttpProjectConfigManager;
            Assert.NotNull(projectConfigManager);

            var actualConfigManagerProps = new ProjectConfigManagerProps(projectConfigManager);
            var expectedConfigManagerProps = new ProjectConfigManagerProps {
                //TODO: Add more properties and then assert.
                Url = "www.testurl.com",
                LastModified = "",
                AutoUpdate = false,
            };

            Assert.IsTrue(TestData.CompareObjects(actualConfigManagerProps, expectedConfigManagerProps));
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
            var expectedConfigManagerProps = new ProjectConfigManagerProps {
                //TODO: Add more properties and then assert.
                Url = "https://cdn.optimizely.com/datafiles/my-sdk-key.json",
                LastModified = "",
                AutoUpdate = false,
            };
            Assert.IsTrue(TestData.CompareObjects(actualConfigManagerProps, expectedConfigManagerProps));
            optimizely.Dispose();
        }

        [Test]
        public void TestProjectConfigManagerWithDatafileAccessToken()
        {
            var optimizely = OptimizelyFactory.NewDefaultInstance("my-sdk-key", null, "access-token");

            // Check values are loaded from app.config or not.
            var projectConfigManager = optimizely.ProjectConfigManager as HttpProjectConfigManager;
            Assert.NotNull(projectConfigManager);

            var actualConfigManagerProps = new ProjectConfigManagerProps(projectConfigManager);
            var expectedConfigManagerProps = new ProjectConfigManagerProps
            {
                //TODO: Add more properties and then assert.
                Url = "https://config.optimizely.com/datafiles/auth/my-sdk-key.json",
                DatafileAccessToken = "access-token",
                LastModified = "",
                AutoUpdate = false,
            };
            Assert.IsTrue(TestData.CompareObjects(actualConfigManagerProps, expectedConfigManagerProps));

            optimizely.Dispose();
        }

        [Test]
        public void TestProjectConfigManagerWithCustomProjectConfigManager()
        {
            var projectConfigManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("10192104166")
                .WithFormat("https://optimizely.com/json/{0}.json")
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .WithStartByDefault()
                .WithAccessToken("access-token")
                .Build();

            var optimizely = OptimizelyFactory.NewDefaultInstance(projectConfigManager);
            var expectedProjectConfigManager = optimizely.ProjectConfigManager as HttpProjectConfigManager;
            var actualConfigManagerProps = new ProjectConfigManagerProps(expectedProjectConfigManager);
            var expectedConfigManagerProps = new ProjectConfigManagerProps
            {
                //TODO: Add more properties and then assert.
                Url = "https://optimizely.com/json/10192104166.json",
                DatafileAccessToken = "access-token",
                LastModified = "",
                AutoUpdate = false,
            };
            Assert.IsTrue(TestData.CompareObjects(actualConfigManagerProps, expectedConfigManagerProps));
            optimizely.Dispose();
        }

        [Test]
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
            Assert.IsTrue(TestData.CompareObjects(actualEventProcessorProps, expectedEventProcessorProps));
            optimizely.Dispose();
        }

        [Test]
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
            Assert.IsTrue(TestData.CompareObjects(actualEventProcessorProps, expectedEventProcessorProps));
            optimizely.Dispose();
        }
    }

}
