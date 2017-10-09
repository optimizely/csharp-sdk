/* 
 * Copyright 2017, Optimizely
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
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using Moq;
using NUnit.Framework;
using OptimizelySDK.Bucketing;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class BucketerTest
    {
        private Mock<ILogger> LoggerMock;
        private ProjectConfig Config;
        private const string TestUserId = "testUserId";
        public string TestBucketingIdControl { get; set; }
        public string TestBucketingIdVariation { get; set; }

        /// <summary>
        /// Bucket Testing helper class
        /// </summary>
        private class BucketerTestItem
        {
            public string UserId { get; set; }
            public string ExperimentId { get; set; }
            public int ExpectedBucketValue { get; set; }
            
            public string BucketingId
            {
                get { return UserId + ExperimentId; }
            }

            public override string ToString()
            {
                return string.Format("UserId: {0}, ExperimentId: {1}, BucketId: {2}, ExpectedBucketValue {3}",
                    UserId, ExperimentId, BucketingId, ExpectedBucketValue);
            }
        }

        [SetUp]
        public void Initialize()
        {
            LoggerMock = new Mock<ILogger>();
            Config = ProjectConfig.Create(TestData.Datafile, LoggerMock.Object, new ErrorHandler.NoOpErrorHandler());
            this.TestBucketingIdControl = "testBucketingIdControl!";  // generates bucketing number 3741
            this.TestBucketingIdVariation = "123456789'"; // generates bucketing number 4567
        }

        [TestFixtureSetUp]
        public void Cleanup()
        {
            LoggerMock = null;
            Config = null;
        }

        [Test]
        public void TestGenerateBucketValue()
        {
            var bucketer = new Bucketer(LoggerMock.Object);

            foreach (var item in new[]
            {
                new BucketerTestItem { UserId = "ppid1", ExperimentId = "1886780721", ExpectedBucketValue = 5254 },
                new BucketerTestItem { UserId = "ppid2", ExperimentId = "1886780721", ExpectedBucketValue = 4299 },
                new BucketerTestItem { UserId = "ppid2", ExperimentId = "1886780722", ExpectedBucketValue = 2434 },
                new BucketerTestItem { UserId = "ppid3", ExperimentId = "1886780721", ExpectedBucketValue = 5439 },
                new BucketerTestItem { UserId = "a very very very very very very very very very very very very very very very long ppd string",
                    ExperimentId = "1886780721", ExpectedBucketValue = 6128 },
            })
            {
                int result = bucketer.GenerateBucketValue(item.BucketingId);
                Assert.AreEqual(item.ExpectedBucketValue, result, 
                    string.Format("Unexpected Bucket Value: [{0}] for [{1}]", result, item));
            }
        }

        [Test]
        public void TestBucketValidExperimentNotInGroup()
        {
            TestBucketer bucketer = new TestBucketer(LoggerMock.Object);
            bucketer.SetBucketValues(new[] { 3000, 7000, 9000 });

            // control
            Assert.AreEqual(new Variation { Id = "7722370027", Key = "control" },
                bucketer.Bucket(Config, Config.GetExperimentFromKey("test_experiment"), TestBucketingIdControl, TestUserId));

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3000] to user [testUserId]"));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [testUserId] is in variation [control] of experiment [test_experiment]."));

            // variation
            Assert.AreEqual(new Variation { Id = "7721010009", Key = "variation" },
                bucketer.Bucket(Config, Config.GetExperimentFromKey("test_experiment"), TestBucketingIdControl, TestUserId));

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(4));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [7000] to user [testUserId]"));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [testUserId] is in variation [variation] of experiment [test_experiment]."));

            // no variation
            Assert.AreEqual(new Variation { },
                bucketer.Bucket(Config, Config.GetExperimentFromKey("test_experiment"), TestBucketingIdControl, TestUserId));

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(6));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [9000] to user [testUserId]"));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [testUserId] is in no variation."));
        }

        [Test]
        public void TestBucketValidExperimentInGroup()
        {
            TestBucketer bucketer = new TestBucketer(LoggerMock.Object);

            // group_experiment_1 (20% experiment)
            // variation 1
            bucketer.SetBucketValues(new[] { 1000, 4000 });
            Assert.AreEqual(new Variation { Id = "7722260071", Key = "group_exp_1_var_1" },
                bucketer.Bucket(Config, Config.GetExperimentFromKey("group_experiment_1"), TestBucketingIdControl, TestUserId));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [1000] to user [testUserId]"));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [testUserId] is in experiment [group_experiment_1] of group [7722400015]."));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [4000] to user [testUserId]"));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [testUserId] is in variation [group_exp_1_var_1] of experiment [group_experiment_1]."));

            // variation 2
            bucketer.SetBucketValues(new[] { 1500, 7000 });
            Assert.AreEqual(new Variation { Id = "7722360022", Key = "group_exp_1_var_2" },
                bucketer.Bucket(Config, Config.GetExperimentFromKey("group_experiment_1"), TestBucketingIdControl, TestUserId));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [1500] to user [testUserId]"));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [testUserId] is in experiment [group_experiment_1] of group [7722400015]."));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [7000] to user [testUserId]"));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [testUserId] is in variation [group_exp_1_var_1] of experiment [group_experiment_1]."));

            // User not in experiment
            bucketer.SetBucketValues(new[] { 5000, 7000 });
            Assert.AreEqual(new Variation { },
                bucketer.Bucket(Config, Config.GetExperimentFromKey("group_experiment_1"), TestBucketingIdControl, TestUserId));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [5000] to user [testUserId]"));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [testUserId] is not in experiment [group_experiment_1] of group [7722400015]."));

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(10));
        }

        [Test]
        public void TestBucketInvalidExperiment()
        {
            var bucketer = new Bucketer(LoggerMock.Object);

            Assert.AreEqual(new Variation { },
                bucketer.Bucket(Config, new Experiment(), TestBucketingIdControl, TestUserId));

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void TestBucketValidExperimentNotInGroupUserInForcedVariation()
        {
            var bucketer = new Bucketer(LoggerMock.Object);

            Assert.AreEqual(new Variation { Id = "7722370027", Key = "control" },
                bucketer.Bucket(Config, Config.GetExperimentFromKey("test_experiment"), TestBucketingIdControl, "user1"));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [user1] is forced into variation [control]."));
            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(1));
        }

        [Test]
        public void TestBucketValidExperimentInGroupUserInForcedVariation()
        {
            var bucketer = new Bucketer(LoggerMock.Object);

            Assert.AreEqual(new Variation { Id = "7722260071", Key = "group_exp_1_var_1" },
                bucketer.Bucket(Config, Config.GetExperimentFromKey("group_experiment_1"), TestBucketingIdControl, "user1"));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [user1] is forced into variation [group_exp_1_var_1]."));
            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(1));
        }
    }
}