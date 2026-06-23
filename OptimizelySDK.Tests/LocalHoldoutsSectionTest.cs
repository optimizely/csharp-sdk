/*
 * Copyright 2026, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.IO;
using System.Linq;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Tests
{
    /// <summary>
    /// Tests for the 'localHoldouts' datafile section: section-based scoping, validation, and backward compat.
    /// </summary>
    [TestFixture]
    public class LocalHoldoutsSectionTest
    {
        private Mock<ILogger> LoggerMock;
        private JObject TestData;

        [SetUp]
        public void Initialize()
        {
            LoggerMock = new Mock<ILogger>();

            var testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestData", "HoldoutTestData.json");
            var jsonContent = File.ReadAllText(testDataPath);
            TestData = JObject.Parse(jsonContent);
        }

        private DatafileProjectConfig BuildConfig(string fixtureName)
        {
            var datafile = TestData[fixtureName].ToString();
            return DatafileProjectConfig.Create(datafile, LoggerMock.Object,
                new NoOpErrorHandler()) as DatafileProjectConfig;
        }

        // -----------------------------------------------------------------------
        // localHoldouts section parsing — happy path
        // -----------------------------------------------------------------------

        [Test]
        public void TestLocalHoldoutsSection_ExposedAsTopLevelProperty()
        {
            var config = BuildConfig("datafileWithLocalHoldoutsSection");

            Assert.IsNotNull(config.LocalHoldouts,
                "LocalHoldouts property should be exposed on DatafileProjectConfig");
            Assert.AreEqual(1, config.LocalHoldouts.Length,
                "LocalHoldouts should contain only valid entries (invalid ones are excluded at parse time)");
        }

        [Test]
        public void TestLocalHoldoutsSection_RegistersInRuleMap()
        {
            // Entries in 'localHoldouts' must be registered under each rule in IncludedRules.
            var config = BuildConfig("datafileWithLocalHoldoutsSection");

            var ruleAHoldouts = config.GetHoldoutsForRule("rule_a");
            Assert.AreEqual(1, ruleAHoldouts.Count,
                "rule_a should be targeted by exactly one local holdout");
            Assert.AreEqual("section_local_1", ruleAHoldouts[0].Id,
                "rule_a should be targeted by section_local_1");
        }

        [Test]
        public void TestLocalHoldoutsSection_EntriesExcludedFromGlobalList()
        {
            // Entries in 'localHoldouts' must NOT appear in GetGlobalHoldouts().
            var config = BuildConfig("datafileWithLocalHoldoutsSection");

            var globals = config.GetGlobalHoldouts();
            Assert.IsFalse(globals.Any(h => h.Id == "section_local_1"),
                "Local holdout must not appear in the global holdouts list");
        }

        [Test]
        public void TestLocalHoldoutsSection_LocalEntryIsRetrievableById()
        {
            // Entries from both sections must be tracked in the unified holdout id map.
            var config = BuildConfig("datafileWithLocalHoldoutsSection");

            Assert.IsNotNull(config.GetHoldout("section_global_1"),
                "Global-section holdout should be retrievable by ID");
            Assert.IsNotNull(config.GetHoldout("section_local_1"),
                "Local-section holdout should be retrievable by ID");
        }

        // -----------------------------------------------------------------------
        // localHoldouts section — variation maps must include local holdouts
        // -----------------------------------------------------------------------

        [Test]
        public void TestLocalHoldoutsSection_VariationsRegisteredInVariationMaps()
        {
            // The decision service resolves variations by holdout key, so local holdouts'
            // variations must be present in the variation maps.
            var config = BuildConfig("datafileWithLocalHoldoutsSection");

            Assert.IsTrue(config.VariationKeyMap.ContainsKey("section_local_holdout_1"),
                "Local holdout key should be present in VariationKeyMap");
            Assert.IsTrue(config.VariationKeyMap["section_local_holdout_1"]
                    .ContainsKey("section_local_off"),
                "Local holdout's variation key should be registered");
        }

        // -----------------------------------------------------------------------
        // Section partition — IncludedRules on global-section entries is ignored
        // -----------------------------------------------------------------------

        [Test]
        public void TestGlobalSection_IncludedRulesOnEntryIsStrippedAtParseTime()
        {
            // Any IncludedRules field on a 'holdouts' entry must NOT narrow its scope.
            // Section membership in 'holdouts' alone determines global scope.
            var config = BuildConfig("datafileWithLocalHoldoutsSection");

            var strayHoldout = config.GetHoldout("section_global_with_stray_rules");
            Assert.IsNotNull(strayHoldout,
                "Global-section holdout with stray includedRules should still be loaded");

            Assert.IsNull(strayHoldout.IncludedRules,
                "IncludedRules on a global-section entry must be stripped at parse time");
            Assert.IsTrue(strayHoldout.IsGlobal,
                "Global-section entry must always classify as global, regardless of source IncludedRules");

            // Must NOT appear under the rule it spuriously listed
            var holdoutsForStrayRule = config.GetHoldoutsForRule("should_be_ignored_rule");
            Assert.AreEqual(0, holdoutsForStrayRule.Count,
                "Stray includedRules on a global-section entry must not place it in the rule map");

            // Must appear in the global list
            var globals = config.GetGlobalHoldouts();
            Assert.IsTrue(globals.Any(h => h.Id == "section_global_with_stray_rules"),
                "Global-section entry with stray includedRules must still be in the global list");
        }

        // -----------------------------------------------------------------------
        // Invalid local holdouts — missing IncludedRules is logged and excluded
        // -----------------------------------------------------------------------

        [Test]
        public void TestLocalHoldoutsSection_MissingIncludedRulesIsExcluded()
        {
            // Entries in 'localHoldouts' without IncludedRules are invalid per spec.
            // SDK must exclude them from evaluation. It must NOT fall back to global application.
            var config = BuildConfig("datafileWithLocalHoldoutsSection");

            // Invalid entry must not be retrievable by ID (excluded from holdout map).
            Assert.IsNull(config.GetHoldout("section_local_invalid"),
                "Invalid local holdout (missing IncludedRules) must be excluded from holdout map");

            // Invalid entry must not be applied as global.
            var globals = config.GetGlobalHoldouts();
            Assert.IsFalse(globals.Any(h => h.Id == "section_local_invalid"),
                "Invalid local holdout must NOT fall back to global application");
        }

        [Test]
        public void TestLocalHoldoutsSection_MissingIncludedRulesLogsError()
        {
            // Verify an error log is emitted for an invalid local holdout entry.
            BuildConfig("datafileWithLocalHoldoutsSection");

            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR,
                    It.Is<string>(msg =>
                        msg.Contains("section_local_invalid_key") &&
                        msg.Contains("includedRules"))),
                Times.AtLeastOnce(),
                "Expected an error log mentioning the invalid local holdout's key and includedRules");
        }

        // -----------------------------------------------------------------------
        // Backward compatibility — datafiles without 'localHoldouts' section
        // -----------------------------------------------------------------------

        [Test]
        public void TestBackwardCompat_DatafileWithoutLocalHoldoutsSection()
        {
            // Old datafiles that only emit the 'holdouts' section must continue to work.
            // Every entry is global; LocalHoldouts defaults to empty array.
            var config = BuildConfig("datafileWithHoldouts");

            Assert.IsNotNull(config.LocalHoldouts,
                "LocalHoldouts must default to an empty array when the section is absent");
            Assert.AreEqual(0, config.LocalHoldouts.Length,
                "Missing 'localHoldouts' key should result in an empty LocalHoldouts array");

            // The 'holdouts' entries are still loaded as global, and no errors are produced.
            Assert.IsTrue(config.GetGlobalHoldouts().Count > 0,
                "Global holdouts from the 'holdouts' section should still be loaded");
        }

        // -----------------------------------------------------------------------
        // Direct DatafileProjectConfig.Create with minimal inline datafiles
        // -----------------------------------------------------------------------

        [Test]
        public void TestBackwardCompat_DatafileMissingBothHoldoutSections()
        {
            // Datafile that emits neither 'holdouts' nor 'localHoldouts' must produce empty lists.
            var datafile = @"{
                ""version"": ""4"",
                ""projectId"": ""p1"",
                ""accountId"": ""a1"",
                ""revision"": ""1"",
                ""experiments"": [],
                ""groups"": [],
                ""attributes"": [],
                ""audiences"": [],
                ""events"": [],
                ""featureFlags"": [],
                ""rollouts"": [],
                ""anonymizeIP"": false
            }";

            var config = DatafileProjectConfig.Create(datafile, LoggerMock.Object,
                new NoOpErrorHandler()) as DatafileProjectConfig;

            Assert.IsNotNull(config);
            Assert.IsNotNull(config.Holdouts);
            Assert.AreEqual(0, config.Holdouts.Length);
            Assert.IsNotNull(config.LocalHoldouts);
            Assert.AreEqual(0, config.LocalHoldouts.Length);
            Assert.AreEqual(0, config.GetGlobalHoldouts().Count);
            Assert.AreEqual(0, config.GetHoldoutsForRule("any_rule").Count);
        }

        [Test]
        public void TestBothSectionsPartitionCorrectly()
        {
            // When both 'holdouts' and 'localHoldouts' are present, scope is enforced by
            // section membership — entries never cross over.
            var datafile = @"{
                ""version"": ""4"",
                ""projectId"": ""p1"",
                ""accountId"": ""a1"",
                ""revision"": ""1"",
                ""experiments"": [],
                ""groups"": [],
                ""attributes"": [],
                ""audiences"": [],
                ""events"": [],
                ""featureFlags"": [],
                ""rollouts"": [],
                ""anonymizeIP"": false,
                ""holdouts"": [
                    {""id"": ""g1"", ""key"": ""g1k"", ""status"": ""Running"",
                     ""variations"": [], ""trafficAllocation"": [],
                     ""audienceIds"": [], ""audienceConditions"": []},
                    {""id"": ""g2"", ""key"": ""g2k"", ""status"": ""Running"",
                     ""variations"": [], ""trafficAllocation"": [],
                     ""audienceIds"": [], ""audienceConditions"": []}
                ],
                ""localHoldouts"": [
                    {""id"": ""l1"", ""key"": ""l1k"", ""status"": ""Running"",
                     ""variations"": [], ""trafficAllocation"": [],
                     ""audienceIds"": [], ""audienceConditions"": [],
                     ""includedRules"": [""rule_a""]},
                    {""id"": ""l2"", ""key"": ""l2k"", ""status"": ""Running"",
                     ""variations"": [], ""trafficAllocation"": [],
                     ""audienceIds"": [], ""audienceConditions"": [],
                     ""includedRules"": [""rule_b""]}
                ]
            }";

            var config = DatafileProjectConfig.Create(datafile, LoggerMock.Object,
                new NoOpErrorHandler()) as DatafileProjectConfig;

            var globalIds = config.GetGlobalHoldouts().Select(h => h.Id).OrderBy(s => s).ToArray();
            CollectionAssert.AreEqual(new[] { "g1", "g2" }, globalIds,
                "Global section entries must be exactly the 'holdouts' entries");

            Assert.AreEqual(1, config.GetHoldoutsForRule("rule_a").Count);
            Assert.AreEqual("l1", config.GetHoldoutsForRule("rule_a")[0].Id);
            Assert.AreEqual(1, config.GetHoldoutsForRule("rule_b").Count);
            Assert.AreEqual("l2", config.GetHoldoutsForRule("rule_b")[0].Id);
        }

        [Test]
        public void TestLocalSection_EmptyIncludedRulesIsValid_TargetsNoRules()
        {
            // IncludedRules == [] is valid (entity is stored), but targets no rules.
            // Not invalid, not global — matches existing entity-level semantics where [] != null.
            var datafile = @"{
                ""version"": ""4"",
                ""projectId"": ""p1"",
                ""accountId"": ""a1"",
                ""revision"": ""1"",
                ""experiments"": [],
                ""groups"": [],
                ""attributes"": [],
                ""audiences"": [],
                ""events"": [],
                ""featureFlags"": [],
                ""rollouts"": [],
                ""anonymizeIP"": false,
                ""localHoldouts"": [
                    {""id"": ""l_empty"", ""key"": ""l_empty_k"", ""status"": ""Running"",
                     ""variations"": [], ""trafficAllocation"": [],
                     ""audienceIds"": [], ""audienceConditions"": [],
                     ""includedRules"": []}
                ]
            }";

            var config = DatafileProjectConfig.Create(datafile, LoggerMock.Object,
                new NoOpErrorHandler()) as DatafileProjectConfig;

            // Stored (valid) — retrievable by id
            var stored = config.GetHoldout("l_empty");
            Assert.IsNotNull(stored, "Local holdout with empty IncludedRules must still be stored");
            Assert.IsFalse(stored.IsGlobal, "Empty IncludedRules is local, not global");

            // Not in any rule map
            Assert.AreEqual(0, config.GetHoldoutsForRule("any_rule").Count,
                "Empty IncludedRules must match no rules");

            // Not global
            Assert.AreEqual(0, config.GetGlobalHoldouts().Count,
                "Local holdout with empty IncludedRules must not be promoted to global");
        }
    }
}
