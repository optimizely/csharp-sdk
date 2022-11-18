/* 
 * Copyright 2019, 2022 Optimizely
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
using OptimizelySDK.Tests.DatafileManagementTests;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace OptimizelySDK.Tests.DatafileManagement_Tests
{
    [TestFixture]
    public class PollingProjectConfigManagerTest
    {
        private Mock<ILogger> _loggerMock;
        private ProjectConfig _projectConfig;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
            _loggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            _projectConfig =
                DatafileProjectConfig.Create(TestData.Datafile, _loggerMock.Object, null);
        }

        [Test]
        public void TestPollingConfigManagerDoesNotBlockWhenProjectConfigIsAlreadyProvided()
        {
            var stopwatch = new Stopwatch();
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3), true, _loggerMock.Object, new int[]
                    { });
            configManager.SetConfig(_projectConfig);

            stopwatch.Start();
            var config = configManager.GetConfig();
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed.Seconds == 0);
            Assert.NotNull(config);
            configManager.Dispose();
        }

        [Test]
        public void TestPollingConfigManagerBlocksWhenProjectConfigIsNotProvided()
        {
            var stopwatch = new Stopwatch();
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(2), true, _loggerMock.Object, new []
                {
                    500,
                });

            stopwatch.Start();
            configManager.GetConfig();
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed.TotalMilliseconds >= 200);
            configManager.Dispose();
        }

        [Test]
        public void TestImmediatelyCalledScheduledRequestIfPreviousRequestDelayedInResponse()
        {
            // period to call is one second
            // Giving response in 1200 milliseconds
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(1500), true, _loggerMock.Object, new []
                {
                    1200, 500, 500,
                });

            configManager.Start();
            System.Threading.Tasks.Task.Delay(50).Wait();
            //Thread.Sleep(50);
            Assert.AreEqual(1, configManager.Counter);
            System.Threading.Tasks.Task.Delay(1000).Wait();
            //Thread.Sleep(1000);
            Assert.AreEqual(1, configManager.Counter);
            System.Threading.Tasks.Task.Delay(200).Wait();
            // Should be called immediately after 1200 seconds. Here checking after 1300 secs.
            //Thread.Sleep(200);
            Assert.AreEqual(2, configManager.Counter);
            configManager.Dispose();
        }

        [Test]
        public void TestTimedOutIfTakingMoreThanBlockingTimeout()
        {
            // period to call is one second
            // Giving response in 1200 milliseconds
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(3),
                TimeSpan.FromMilliseconds(1000), true, _loggerMock.Object, new []
                {
                    1300, 500, 500,
                });

            configManager.Start();
            configManager.GetConfig();
            _loggerMock.Verify(l => l.Log(LogLevel.WARN,
                "Timeout exceeded waiting for ProjectConfig to be set, returning null."));
            configManager.Dispose();
        }

        [Test]
        public void TestTimedOutOnlyIfSchedulerStarted()
        {
            // period to call is 3 second
            // Giving response in 1200 milliseconds and timeout should be in 1000 milliseconds
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(3),
                TimeSpan.FromMilliseconds(1000), true, _loggerMock.Object, new []
                {
                    1300, 500, 500,
                });
            Stopwatch sw = new Stopwatch();
            sw.Start();
            configManager.GetConfig();
            sw.Stop();
            Assert.GreaterOrEqual(sw.Elapsed.TotalMilliseconds, 950);
            configManager.Dispose();
        }

        [Test]
        public void TestDontTimedOutIfSchedulerNotStarted()
        {
            // period to call is 3 second
            // Giving response in 1200 milliseconds and timeout should be in 1000 milliseconds
            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(3),
                TimeSpan.FromMilliseconds(1000), true, _loggerMock.Object, new []
                {
                    1300, 500, 500,
                });
            Stopwatch sw = new Stopwatch();
            sw.Start();
            configManager.GetConfig();
            sw.Stop();
            Assert.GreaterOrEqual(sw.Elapsed.TotalMilliseconds, 1000);
            configManager.Dispose();
        }

        [Test]
        public void TestReturnDatafileImmediatelyOnceGetValidDatafileRemotely()
        {
            var projConfig =
                DatafileProjectConfig.Create(TestData.Datafile, _loggerMock.Object, null);
            var data = new List<TestPollingData>()
            {
                new TestPollingData
                {
                    PollingTime = 500,
                    ChangeVersion = false,
                    ConfigDatafile = projConfig
                },
                new TestPollingData
                {
                    PollingTime = 500,
                    ChangeVersion = false,
                    ConfigDatafile = projConfig
                },
            };

            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromSeconds(3),
                TimeSpan.FromMilliseconds(5000), true, _loggerMock.Object, data.ToArray());

            var config = configManager.GetConfig();
            Assert.NotNull(config);
            Assert.AreEqual(1, configManager.Counter);
            configManager.Dispose();
        }

        [Test]
        public void TestWaitUntilValidDatafileIsNotGiven()
        {
            // Send invalid datafile. 
            // Wait for one more poll
            // Send invalid datafile. 
            // wait for one more poll
            // then send the right datafile
            // see it should release blocking.
            // blocking timeout must be infinite.
            var projConfig =
                DatafileProjectConfig.Create(TestData.Datafile, _loggerMock.Object, null);
            var data = new List<TestPollingData>()
            {
                new TestPollingData
                {
                    PollingTime = 50,
                    ChangeVersion = false,
                    ConfigDatafile = null,
                },
                new TestPollingData
                {
                    PollingTime = 50,
                    ChangeVersion = false,
                    ConfigDatafile = null,
                },
                new TestPollingData
                {
                    PollingTime = 50,
                    ChangeVersion = false,
                    ConfigDatafile = projConfig,
                }
            };


            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(10000), true, _loggerMock.Object, data.ToArray());
            configManager.Start();
            // after 3rd attempt should get 
            configManager.GetConfig();
            //Assert.NotNull(config);
            Assert.AreEqual(3, configManager.Counter);
            configManager.Dispose();
        }


        [Test]
        public void TestWaitUntilValidDatafileIsNotGivenOrTimedOut()
        {
            var data = new List<TestPollingData>()
            {
                new TestPollingData
                {
                    PollingTime = 50,
                    ChangeVersion = false,
                    ConfigDatafile = null,
                },
                new TestPollingData
                {
                    PollingTime = 50,
                    ChangeVersion = false,
                    ConfigDatafile = null,
                },
                new TestPollingData
                {
                    PollingTime = 50,
                    ChangeVersion = false,
                    ConfigDatafile = null,
                }
            };

            var configManager = new TestPollingProjectConfigManager(TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(2500), true, _loggerMock.Object, data.ToArray());
            configManager.Start();
            // after 3rd attempt should be released with null.
            var config = configManager.GetConfig();
            Assert.Null(config);
            Assert.AreEqual(3, configManager.Counter);
            configManager.Dispose();
        }
    }
}
