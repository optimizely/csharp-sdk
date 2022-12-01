/* 
 * Copyright 2022 Optimizely
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
using OptimizelySDK.Logger;
using OptimizelySDK.Odp;
using OptimizelySDK.Odp.Entity;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.Tests.OdpTests
{
    [TestFixture]
    public class OdpConfigTest
    {
        private const string API_KEY = "UrAp1k3Y";
        private const string API_HOST = "https://not-real-odp-host.example.com";

        private static readonly List<string> segmentsToCheck = new List<string>
        {
            "UPPER-CASE-AUDIENCE",
            "lower-case-audience",
        };

        private readonly OdpConfig _goodOdpConfig =
            new OdpConfig(API_KEY, API_HOST, segmentsToCheck);

        [Test]
        public void ShouldNotEqualWithNullInApiKey()
        {
            var nullKeyConfig =
                new OdpConfig(null, API_HOST, segmentsToCheck);

            Assert.IsFalse(_goodOdpConfig.Equals(nullKeyConfig));
            Assert.IsFalse(nullKeyConfig.Equals(_goodOdpConfig));
        }

        [Test]
        public void ShouldNotEqualWithNullInApiHost()
        {
            var nullHostConfig =
                new OdpConfig(API_KEY, null, segmentsToCheck);

            Assert.IsFalse(_goodOdpConfig.Equals(nullHostConfig));
            Assert.IsFalse(nullHostConfig.Equals(_goodOdpConfig));
        }

        [Test]
        public void ShouldNotEqualWithNullSegmentsCollection()
        {
            var nullSegmentsConfig =
                new OdpConfig(API_KEY, API_HOST, null);

            Assert.IsFalse(_goodOdpConfig.Equals(nullSegmentsConfig));
            Assert.IsFalse(nullSegmentsConfig.Equals(_goodOdpConfig));
        }

        [Test]
        public void ShouldNotEqualWithSegmentsWithNull()
        {
            var segmentsWithANullValue =
                new OdpConfig(API_KEY, API_HOST, new List<string>
                {
                    "good-value",
                    null,
                });

            Assert.IsFalse(_goodOdpConfig.Equals(segmentsWithANullValue));
            Assert.IsFalse(segmentsWithANullValue.Equals(_goodOdpConfig));
        }

        [Test]
        public void ShouldEqualDespiteCaseDifferenceInApiHost()
        {
            var apiHostUpperCasedConfig = new OdpConfig(API_KEY, API_HOST.ToUpper(), segmentsToCheck);

            Assert.IsTrue(_goodOdpConfig.Equals(apiHostUpperCasedConfig));
            Assert.IsTrue(apiHostUpperCasedConfig.Equals(_goodOdpConfig));
        }

        [Test]
        public void ShouldEqualDespiteCaseDifferenceInSegments()
        {
            var wrongCaseSegmentsToCheck = new List<string>
            {
                "upper-case-audience",
                "LOWER-CASE-AUDIENCE",
            };
            var wrongCaseConfig = new OdpConfig(API_KEY, API_HOST, wrongCaseSegmentsToCheck);
            
            Assert.IsTrue(_goodOdpConfig.Equals(wrongCaseConfig));
            Assert.IsTrue(wrongCaseConfig.Equals(_goodOdpConfig));
        }
    }
}
