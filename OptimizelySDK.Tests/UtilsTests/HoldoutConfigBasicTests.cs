/*
 * Copyright 2025, Optimizely
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

using NUnit.Framework;
using OptimizelySDK.Entity;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class HoldoutConfigBasicTests
    {
        [Test]
        public void TestEmptyHoldouts_ShouldHaveEmptyMaps()
        {
            var config = new HoldoutConfig(new Holdout[0]);

            Assert.IsNotNull(config.HoldoutIdMap);
            Assert.AreEqual(0, config.HoldoutIdMap.Count);
            Assert.AreEqual(0, config.HoldoutCount);
        }

        [Test]
        public void TestHoldoutIdMapping()
        {
            var holdout1 = CreateTestHoldout("holdout_1", "h1");
            var holdout2 = CreateTestHoldout("holdout_2", "h2");
            var allHoldouts = new[] { holdout1, holdout2 };
            var config = new HoldoutConfig(allHoldouts);

            Assert.IsNotNull(config.HoldoutIdMap);
            Assert.AreEqual(2, config.HoldoutIdMap.Count);
            Assert.AreEqual(2, config.HoldoutCount);

            Assert.IsTrue(config.HoldoutIdMap.ContainsKey("holdout_1"));
            Assert.IsTrue(config.HoldoutIdMap.ContainsKey("holdout_2"));

            Assert.AreEqual(holdout1.Id, config.HoldoutIdMap["holdout_1"].Id);
            Assert.AreEqual(holdout2.Id, config.HoldoutIdMap["holdout_2"].Id);
        }

        [Test]
        public void TestGetHoldoutById()
        {
            var holdout = CreateTestHoldout("holdout_1", "h1");
            var config = new HoldoutConfig(new[] { holdout });

            var retrieved = config.GetHoldout("holdout_1");

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("holdout_1", retrieved.Id);
            Assert.AreEqual("h1", retrieved.Key);
        }

        [Test]
        public void TestGetHoldoutById_InvalidId()
        {
            var holdout = CreateTestHoldout("holdout_1", "h1");
            var config = new HoldoutConfig(new[] { holdout });

            var result = config.GetHoldout("invalid_id");
            Assert.IsNull(result);
        }

        [Test]
        public void TestGetHoldoutById_NullId()
        {
            var holdout = CreateTestHoldout("holdout_1", "h1");
            var config = new HoldoutConfig(new[] { holdout });

            var result = config.GetHoldout(null);
            Assert.IsNull(result);
        }

        [Test]
        public void TestGetHoldoutById_EmptyId()
        {
            var holdout = CreateTestHoldout("holdout_1", "h1");
            var config = new HoldoutConfig(new[] { holdout });

            var result = config.GetHoldout("");
            Assert.IsNull(result);
        }

        [Test]
        public void TestUpdateHoldoutMapping()
        {
            var holdout1 = CreateTestHoldout("holdout_1", "h1");
            var config = new HoldoutConfig(new[] { holdout1 });

            // Initial state
            Assert.AreEqual(1, config.HoldoutIdMap.Count);
            Assert.AreEqual(1, config.HoldoutCount);

            // Update with new holdouts
            var holdout2 = CreateTestHoldout("holdout_2", "h2");
            config.UpdateHoldoutMapping(new[] { holdout1, holdout2 });

            Assert.AreEqual(2, config.HoldoutIdMap.Count);
            Assert.AreEqual(2, config.HoldoutCount);
            Assert.IsTrue(config.HoldoutIdMap.ContainsKey("holdout_1"));
            Assert.IsTrue(config.HoldoutIdMap.ContainsKey("holdout_2"));
        }

        [Test]
        public void TestNullHoldouts()
        {
            var config = new HoldoutConfig(null);

            Assert.IsNotNull(config.HoldoutIdMap);
            Assert.AreEqual(0, config.HoldoutIdMap.Count);
            Assert.AreEqual(0, config.HoldoutCount);
        }

        // Helper method to create test holdouts
        private Holdout CreateTestHoldout(string id, string key)
        {
            return new Holdout
            {
                Id = id,
                Key = key,
                Status = "Running",
                Variations = new Variation[0],
                TrafficAllocation = new TrafficAllocation[0],
                AudienceIds = new string[0],
                AudienceConditions = null
            };
        }
    }
}
