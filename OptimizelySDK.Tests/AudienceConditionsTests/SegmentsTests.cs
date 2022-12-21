/* 
 * Copyright 2022, Optimizely
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

using Moq;
using NUnit.Framework;
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.Tests.AudienceConditionsTests
{
    [TestFixture]
    public class SegmentsTests
    {
        private BaseCondition _qualifiedMatchCondition;
        private BaseCondition _anotherQualifiedMatchCondition;
        private ICondition _exactMatchCondition;
        private Mock<ILogger> _mockLogger;

        private const string QUALIFIED_MATCH_VALUE = "example_odp_condition_value";
        private const string ANOTHER_QUALIFIED_MATCH_VALUE = "another_example_odp_condition_value";

        [TestFixtureSetUp]
        public void Setup()
        {
            _qualifiedMatchCondition = new BaseCondition
            {
                Value = QUALIFIED_MATCH_VALUE,
                Type = "third_party_dimension",
                Name = "odp.audiences",
                Match = "qualified",
            };

            _anotherQualifiedMatchCondition = new BaseCondition
            {
                Value = ANOTHER_QUALIFIED_MATCH_VALUE,
                Type = "third_party_dimension",
                Name = "odp.audiences",
                Match = "qualified",
            };

            _exactMatchCondition = new BaseCondition()
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

            var actualSegments = new SortedSet<string>(allSegments);
            Assert.AreEqual(expectedSegments, actualSegments);
        }

        [Test]
        public void ShouldFindOdpSegmentFromAndCondition()
        {
            var conditions = new AndCondition
            {
                Conditions = new[]
                {
                    _qualifiedMatchCondition, _exactMatchCondition,
                },
            };

            var allSegments = Audience.GetSegments(conditions);

            Assert.AreEqual(_qualifiedMatchCondition.Value.ToString(),
                allSegments.FirstOrDefault());
        }

        [Test]
        public void ShouldFindOdpSegmentFromOrCondition()
        {
            var conditions = new OrCondition
            {
                Conditions = new[]
                {
                    _exactMatchCondition, _qualifiedMatchCondition,
                },
            };

            var allSegments = Audience.GetSegments(conditions);

            Assert.AreEqual(_qualifiedMatchCondition.Value.ToString(),
                allSegments.FirstOrDefault());
        }

        [Test]
        public void ShouldNotFindOdpSegmentsFromConditions()
        {
            var conditions = new AndCondition
            {
                Conditions = new[]
                {
                    _exactMatchCondition, _exactMatchCondition, _exactMatchCondition,
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
                    _qualifiedMatchCondition, _exactMatchCondition,
                },
            };
            var twoQualified = new AndCondition
            {
                Conditions = new ICondition[]
                {
                    _anotherQualifiedMatchCondition, _qualifiedMatchCondition,
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
            Assert.Contains(QUALIFIED_MATCH_VALUE, allSegments);
            Assert.Contains(ANOTHER_QUALIFIED_MATCH_VALUE, allSegments);
        }
    }
}
