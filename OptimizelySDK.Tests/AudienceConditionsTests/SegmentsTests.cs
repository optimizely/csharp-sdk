/* 
 * Copyright 2022-2023 Optimizely
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

using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Tests.AudienceConditionsTests
{
    [TestFixture]
    public class SegmentsTests
    {
        private BaseCondition _firstThirdPartyOdpQualifiedMatchCondition;
        private BaseCondition _secondThirdPartyOdpQualifiedMatchCondition;
        private ICondition _customExactMatchCondition;
        private Mock<ILogger> _mockLogger;

        private const string FIRST_CONDITION_VALUE = "first_condition_value";
        private const string SECOND_CONDITION_VALUE = "second_condition_value";

        [TestFixtureSetUp]
        public void Setup()
        {
            _firstThirdPartyOdpQualifiedMatchCondition = new BaseCondition
            {
                Value = FIRST_CONDITION_VALUE,
                Type = "third_party_dimension",
                Name = "odp.audiences",
                Match = "qualified",
            };

            _secondThirdPartyOdpQualifiedMatchCondition = new BaseCondition
            {
                Value = SECOND_CONDITION_VALUE,
                Type = "third_party_dimension",
                Name = "odp.audiences",
                Match = "qualified",
            };

            _customExactMatchCondition = new BaseCondition()
            {
                Value = "test_custom_value",
                Type = "custom_attribute",
                Name = "test_custom_name",
                Match = "exact",
            };

            _mockLogger = new Mock<ILogger>();
            _mockLogger.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
        }

        [Test]
        public void ShouldGetSegmentsFromDatafileTypedAudiences()
        {
            var expectedSegments = new SortedSet<string>
            {
                "ats_bug_bash_segment_gender",
                "ats_bug_bash_segment_has_purchased",
                "has_email_opted_out",
                "ats_bug_bash_segment_dob",
            };
            var optimizelyClient = new Optimizely(TestData.OdpSegmentsDatafile,
                new ValidEventDispatcher(), _mockLogger.Object);

            var allSegments = optimizelyClient.ProjectConfigManager.GetConfig().Segments;

            var orderedDistinctSegments = new SortedSet<string>(allSegments);
            // check for no duplicates
            Assert.AreEqual(allSegments.Length, orderedDistinctSegments.Count);
            Assert.AreEqual(expectedSegments, orderedDistinctSegments);
        }

        [Test]
        public void ShouldFindOdpSegmentFromAndCondition()
        {
            var conditions = new AndCondition
            {
                Conditions = new[]
                {
                    _firstThirdPartyOdpQualifiedMatchCondition, _customExactMatchCondition,
                },
            };

            var allSegments = Audience.GetSegments(conditions);

            Assert.AreEqual(_firstThirdPartyOdpQualifiedMatchCondition.Value.ToString(),
                allSegments.FirstOrDefault());
        }

        [Test]
        public void ShouldFindOdpSegmentFromOrCondition()
        {
            var conditions = new OrCondition
            {
                Conditions = new[]
                {
                    _customExactMatchCondition, _firstThirdPartyOdpQualifiedMatchCondition,
                },
            };

            var allSegments = Audience.GetSegments(conditions);

            Assert.AreEqual(_firstThirdPartyOdpQualifiedMatchCondition.Value.ToString(),
                allSegments.FirstOrDefault());
        }

        [Test]
        public void ShouldNotFindOdpSegmentsFromConditions()
        {
            var conditions = new AndCondition
            {
                Conditions = new[]
                {
                    _customExactMatchCondition, _customExactMatchCondition,
                    _customExactMatchCondition,
                },
            };

            var allSegments = Audience.GetSegments(conditions);

            Assert.IsEmpty(allSegments);
        }

        [Test]
        public void ShouldFindAndDedupeNestedOdpSegments()
        {
            var qualifiedAndExact = new AndCondition
            {
                Conditions = new ICondition[]
                {
                    _firstThirdPartyOdpQualifiedMatchCondition, _customExactMatchCondition,
                },
            };
            var twoQualified = new AndCondition
            {
                Conditions = new ICondition[]
                {
                    _secondThirdPartyOdpQualifiedMatchCondition,
                    _firstThirdPartyOdpQualifiedMatchCondition,
                },
            };
            var orConditions = new OrCondition
            {
                Conditions = new ICondition[]
                {
                    qualifiedAndExact, twoQualified,
                },
            };
            var notCondition = new NotCondition
            {
                Condition = orConditions,
            };

            var allSegments = Audience.GetSegments(notCondition).ToList();

            Assert.AreEqual(2, allSegments.Count);
            Assert.Contains(FIRST_CONDITION_VALUE, allSegments);
            Assert.Contains(SECOND_CONDITION_VALUE, allSegments);
        }
    }
}
