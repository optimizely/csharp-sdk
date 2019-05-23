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
using OptimizelySDK.DatafileManagement;
using OptimizelySDK.Logger;
using System;
using System.Diagnostics;
using System.Threading;

namespace OptimizelySDK.Tests.DatafileManagement_Tests
{
    public class TestProjectConfigManager : PollingProjectConfigManager
    {
        public TestProjectConfigManager(TimeSpan period, ILogger logger) : base(period, TimeSpan.Zero, logger)
        {

        }

        protected override ProjectConfig Poll()
        {
            Thread.Sleep(2000);
            return DatafileProjectConfig.Create(TestData.TypedAudienceDatafile, null, null);
        }
    }

    [TestFixture]
    public class PollingProjectConfigManagerTest
    {
        private Mock<ILogger> LoggerMock;
        private TestProjectConfigManager TestProjectConfigManager;

        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            TestProjectConfigManager = new TestProjectConfigManager(TimeSpan.FromSeconds(3), LoggerMock.Object);
        }

        [Test]
        public void TestPollingConfigManagerBlocksForProjectConfigWhenStarted()
        {
            var stopwatch = new Stopwatch();
            var configManager = new TestProjectConfigManager(TimeSpan.FromSeconds(2), LoggerMock.Object);

            stopwatch.Start();
            var config = configManager.GetConfig();
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed.Seconds >= 2);
            Assert.NotNull(config);
        }

        [Test]
        public void TestPollingConfigManagerGetConfigWithDefault()
        {
            var config = DatafileProjectConfig.Create(TestData.TypedAudienceDatafile, null, null);
            var configManager = new TestProjectConfigManager(TimeSpan.FromSeconds(2), LoggerMock.Object);
            configManager.SetConfig(config);

            Assert.True(TestData.CompareObjects(configManager.GetConfig(), config));
        }

        [Test]
        public void TestPollingConfigManagerGetConfigNotStarted()
        {
            var config = DatafileProjectConfig.Create(TestData.TypedAudienceDatafile, null, null);
            var configManager = new TestProjectConfigManager(TimeSpan.FromSeconds(2), LoggerMock.Object);
            configManager.SetConfig(config);
            configManager.Stop();

            Assert.False(configManager.IsStarted);
            Assert.True(TestData.CompareObjects(configManager.GetConfig(), config));
        }
    }
}
