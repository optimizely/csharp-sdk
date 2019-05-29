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
using OptimizelySDK.Tests.DatafileManagementTests;
using System;
using System.Diagnostics;
using System.Threading;

namespace OptimizelySDK.Tests.DatafileManagement_Tests
{
    [TestFixture]
    public class PollingProjectConfigManagerTest
    {
        private Mock<ILogger> LoggerMock;
        private ProjectConfig ProjectConfig;

        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            ProjectConfig = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, null);
        }

        [Test]
        public void TestPollingConfigManagerDoesNotBlockWhenProjectConfigIsAlreadyProvided()
        {
            var stopwatch = new Stopwatch();
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3), LoggerMock.Object, null);
            configManager.SetConfig(ProjectConfig);

            stopwatch.Start();
            var config = configManager.GetConfig();
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed.Seconds == 0);
            Assert.NotNull(config);
        }

        [Test]
        public void TestPollingConfigManagerBlocksWhenProjectConfigIsNotProvided()
        {
            var stopwatch = new Stopwatch();
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3), LoggerMock.Object, null);

            stopwatch.Start();
            var config = configManager.GetConfig();
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed.Seconds >= 2);
            Assert.NotNull(config);
        }

        // TODO: GetConfig without start should always block execution.
        [Test]
        public void TestPollingConfigManagerBlockExecutionWithoutStartOnGtConfig()
        {
            var stopwatch = new Stopwatch();
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3), LoggerMock.Object, null);

            stopwatch.Start();
            var config = configManager.GetConfig();
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed.Milliseconds >= 500);
            Assert.NotNull(config);


        }

        [Test]
        public void TestImmediatelyCalledScheduledRequestIfPreviousRequestDelayedInResponse()
        {
            // period to call is one second
            // Giving response in 1200 milliseconds
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1500), LoggerMock.Object, new int[] { 1200, 500, 500 });

            configManager.Start();
            //Thread.Sleep(200);
            Thread.Sleep(50);
            Assert.AreEqual(1, configManager.Counter);
            Thread.Sleep(1000);
            Assert.AreEqual(1, configManager.Counter);
            // Should be called immediately after 1200 seconds. Here checking after 1300 secs.
            Thread.Sleep(200);
            Assert.AreEqual(2, configManager.Counter);

        }

        [Test]
        public void TestTimedoutIfTakingMorethanBlockingTimeout()
        {
            // period to call is one second
            // Giving response in 1200 milliseconds
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(1000), LoggerMock.Object, new int[] { 1300, 500, 500 });

            configManager.Start();
            var config = configManager.GetConfig();
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, "Timeout exceeded waiting for ProjectConfig to be set, returning null."));

        }

        [Test]
        public void TestTimedoutOnlyIfSchedulerStartedOtherwiseDontTimedout()
        {
            // period to call is one second
            // Giving response in 1200 milliseconds
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(1000), LoggerMock.Object, new int[] { 1300, 500, 500 });
            configManager.Stop();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var config = configManager.GetConfig();
            sw.Stop();
            Assert.GreaterOrEqual(sw.Elapsed.TotalMilliseconds, 1300);
        }

        [Test] // Move it to HttpProjectConfig
        public void TestReturnDatafileImmediatelyOnceGetValidDatafileLocally()
        {
            // TODO: Need to add test case.
            Assert.True(false);
        }

        [Test]
        public void TestReturnDatafileImmediatelyOnceGetValidDatafileRemotely()
        {
            // TODO: Need to add test case.
            Assert.True(false);
        }

        [Test]
        public void TestWaitUntilValidDatfileIsNotGiven()
        {
            // Send invalid datafile. 
            // Wait for one more poll
            // Send invalid datafile. 
            // wait for one more poll
            // then send the right datafile
            // see it should release blocking.
            // blocking timeout must be inifinity.
            // TODO: Need to add test case.
            Assert.True(false);
        }
    }
}
