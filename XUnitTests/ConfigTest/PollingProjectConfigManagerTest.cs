/* 
 * Copyright 2020, Optimizely
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
using OptimizelySDK.Config;
using OptimizelySDK.Logger;
using OptimizelySDK.XUnitTests.DatafileManagementTests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace OptimizelySDK.XUnitTests.DatafileManagement_Tests
{
    public class PollingProjectConfigManagerTest
    {
        private Mock<ILogger> LoggerMock;
        private ProjectConfig ProjectConfig;

        public PollingProjectConfigManagerTest()
        {            
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            ProjectConfig = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, null);
        }
        
        [Fact]
        public void TestPollingConfigManagerDoesNotBlockWhenProjectConfigIsAlreadyProvided()
        {
            var stopwatch = new Stopwatch();
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3), true, LoggerMock.Object, new int[] { });
            configManager.SetConfig(ProjectConfig);

            stopwatch.Start();
            var config = configManager.GetConfig();
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed.Seconds == 0);
            Assert.NotNull(config);
            configManager.Dispose();
        }

        [Fact]
        public void TestPollingConfigManagerBlocksWhenProjectConfigIsNotProvided()
        {
            var stopwatch = new Stopwatch();
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), true, LoggerMock.Object, new int[] {500 });

            stopwatch.Start();
            var config = configManager.GetConfig();
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed.TotalMilliseconds >= 500);
            configManager.Dispose();
        }

        [Fact]
        public void TestImmediatelyCalledScheduledRequestIfPreviousRequestDelayedInResponse()
        {
            // period to call is one second
            // Giving response in 1200 milliseconds
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1500), true, LoggerMock.Object, new int[] { 1200, 500, 500 });

            configManager.Start();
            System.Threading.Tasks.Task.Delay(50).Wait();
            //Thread.Sleep(50);
            Assert.Equal(1, configManager.Counter);
            System.Threading.Tasks.Task.Delay(1000).Wait();
            //Thread.Sleep(1000);
            Assert.Equal(1, configManager.Counter);
            System.Threading.Tasks.Task.Delay(200).Wait();
            // Should be called immediately after 1200 seconds. Here checking after 1300 secs.
            //Thread.Sleep(200);
            Assert.Equal(2, configManager.Counter);
            configManager.Dispose();

        }

        [Fact]
        public void TestTimedoutIfTakingMorethanBlockingTimeout()
        {
            // period to call is one second
            // Giving response in 1200 milliseconds
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(1000), true, LoggerMock.Object, new int[] { 1300, 500, 500 });

            configManager.Start();
            var config = configManager.GetConfig();
            LoggerMock.Verify(l => l.Log(LogLevel.WARN, "Timeout exceeded waiting for ProjectConfig to be set, returning null."));
            configManager.Dispose();
        }

        [Fact]
        public void TestTimedoutOnlyIfSchedulerStarted()
        {
            // period to call is 3 second
            // Giving response in 1200 milliseconds and timedout should be in 1000 miliseconds
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(1000), true, LoggerMock.Object, new int[] { 1300, 500, 500 });
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var config = configManager.GetConfig();
            sw.Stop();
            Assert.True(sw.Elapsed.TotalMilliseconds >= 950);
            configManager.Dispose();
        }

        [Fact]
        public void TestDontTimedoutIfSchedulerNotStarted()
        {
            // period to call is 3 second
            // Giving response in 1200 milliseconds and timedout should be in 1000 miliseconds
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(1000), true, LoggerMock.Object, new int[] { 1300, 500, 500 });
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var config = configManager.GetConfig();
            sw.Stop();
            Assert.True(sw.Elapsed.TotalMilliseconds >= 1000);
            configManager.Dispose();
        }

        [Fact]
        public void TestReturnDatafileImmediatelyOnceGetValidDatafileRemotely()
        {
            var projConfig =  DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, null);
            var data = new List<TestPollingData>() {
                new TestPollingData { PollingTime = 500, ChangeVersion = false, ConfigDatafile = projConfig},
                new TestPollingData { PollingTime = 500, ChangeVersion = false, ConfigDatafile = projConfig}
            };

            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(5000), true, LoggerMock.Object, data.ToArray());

            var config = configManager.GetConfig();
            Assert.NotNull(config);
            Assert.Equal(1, configManager.Counter);
            configManager.Dispose();
        }

        [Fact]
        public void TestWaitUntilValidDatfileIsNotGiven()
        {
            // Send invalid datafile. 
            // Wait for one more poll
            // Send invalid datafile. 
            // wait for one more poll
            // then send the right datafile
            // see it should release blocking.
            // blocking timeout must be inifinity.
            var projConfig = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, null);
            var data = new List<TestPollingData>() {
                new TestPollingData { PollingTime = 50, ChangeVersion = false, ConfigDatafile = null},
                new TestPollingData { PollingTime = 50, ChangeVersion = false, ConfigDatafile = null},
                new TestPollingData { PollingTime = 50, ChangeVersion = false, ConfigDatafile = projConfig}
            };


            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(10000), true, LoggerMock.Object, data.ToArray());
            configManager.Start();
            // after 3rd attempt should get 
            var config = configManager.GetConfig();
            //Assert.NotNull(config);
            Assert.Equal(3, configManager.Counter);
            configManager.Dispose();
        }


        [Fact]
        public void TestWaitUntilValidDatafileIsNotGivenOrTimedout()
        {
            var data = new List<TestPollingData>() {
                new TestPollingData { PollingTime = 50, ChangeVersion = false, ConfigDatafile = null},
                new TestPollingData { PollingTime = 50, ChangeVersion = false, ConfigDatafile = null},
                new TestPollingData { PollingTime = 50, ChangeVersion = false, ConfigDatafile = null}
            };

            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(2500), true, LoggerMock.Object, data.ToArray());
            configManager.Start();
            // after 3rd attempt should be released with null.
            var config = configManager.GetConfig();
            Assert.Null(config);
            Assert.Equal(3, configManager.Counter);
            configManager.Dispose();
        }
    }
}
