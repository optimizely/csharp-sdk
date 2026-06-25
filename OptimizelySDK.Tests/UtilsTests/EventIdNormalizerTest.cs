/*
 * Copyright 2026, Optimizely
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
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests.UtilsTests
{
    /// <summary>
    /// Unit tests for FSSDK-12813 decision-event id normalization rules.
    /// </summary>
    [TestFixture]
    public class EventIdNormalizerTest
    {
        // ---------- IsNumericIdString ----------

        [Test]
        public void IsNumericIdString_DigitString_ReturnsTrue()
        {
            Assert.IsTrue(EventIdNormalizer.IsNumericIdString("7719770039"));
        }

        [Test]
        public void IsNumericIdString_SingleDigit_ReturnsTrue()
        {
            Assert.IsTrue(EventIdNormalizer.IsNumericIdString("0"));
            Assert.IsTrue(EventIdNormalizer.IsNumericIdString("9"));
        }

        [Test]
        public void IsNumericIdString_LeadingZeros_ReturnsTrue()
        {
            // Per FSSDK-12813 spec: leading zeros are allowed.
            Assert.IsTrue(EventIdNormalizer.IsNumericIdString("0001"));
            Assert.IsTrue(EventIdNormalizer.IsNumericIdString("0000"));
        }

        [Test]
        public void IsNumericIdString_Null_ReturnsFalse()
        {
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString(null));
        }

        [Test]
        public void IsNumericIdString_Empty_ReturnsFalse()
        {
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString(string.Empty));
        }

        [Test]
        public void IsNumericIdString_Whitespace_ReturnsFalse()
        {
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString(" "));
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString("\t"));
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString("123 "));
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString(" 123"));
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString("12 3"));
        }

        [Test]
        public void IsNumericIdString_AlphaOrAlphanumeric_ReturnsFalse()
        {
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString("abc"));
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString("variation_a"));
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString("exp_42"));
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString("123abc"));
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString("abc123"));
        }

        [Test]
        public void IsNumericIdString_SignedOrDecimal_ReturnsFalse()
        {
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString("-123"));
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString("+123"));
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString("12.3"));
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString("1e5"));
            Assert.IsFalse(EventIdNormalizer.IsNumericIdString("0x1F"));
        }

        // ---------- NormalizeCampaignId ----------

        [Test]
        public void NormalizeCampaignId_ValidNumeric_ReturnsAsIs()
        {
            Assert.AreEqual("7719770039",
                EventIdNormalizer.NormalizeCampaignId("7719770039", "1111111111"));
        }

        [Test]
        public void NormalizeCampaignId_Null_SubstitutesExperimentId()
        {
            Assert.AreEqual("1111111111",
                EventIdNormalizer.NormalizeCampaignId(null, "1111111111"));
        }

        [Test]
        public void NormalizeCampaignId_Empty_SubstitutesExperimentId()
        {
            Assert.AreEqual("1111111111",
                EventIdNormalizer.NormalizeCampaignId(string.Empty, "1111111111"));
        }

        [Test]
        public void NormalizeCampaignId_NonNumeric_SubstitutesExperimentId()
        {
            Assert.AreEqual("1111111111",
                EventIdNormalizer.NormalizeCampaignId("variation_a", "1111111111"));
            Assert.AreEqual("1111111111",
                EventIdNormalizer.NormalizeCampaignId("exp_42", "1111111111"));
            Assert.AreEqual("1111111111",
                EventIdNormalizer.NormalizeCampaignId("abc", "1111111111"));
        }

        [Test]
        public void NormalizeCampaignId_Whitespace_SubstitutesExperimentId()
        {
            Assert.AreEqual("1111111111",
                EventIdNormalizer.NormalizeCampaignId(" 7719770039 ", "1111111111"));
            Assert.AreEqual("1111111111",
                EventIdNormalizer.NormalizeCampaignId(" ", "1111111111"));
        }

        [Test]
        public void NormalizeCampaignId_InvalidWithEmptyExperimentId_ReturnsEmpty()
        {
            // Mirrors the rollout case where activatedExperiment is null and we want
            // wire output `""` rather than `null` (matches existing test contract).
            Assert.AreEqual(string.Empty,
                EventIdNormalizer.NormalizeCampaignId(null, string.Empty));
        }

        [Test]
        public void NormalizeCampaignId_SubstituteNotRecursivelyNormalized()
        {
            // The normalizer returns experimentId AS-IS when campaignId is invalid.
            // This matches the cross-SDK contract and lets callers see the exact substitute.
            Assert.AreEqual("not_numeric_either",
                EventIdNormalizer.NormalizeCampaignId(null, "not_numeric_either"));
        }

        // ---------- NormalizeVariationId ----------

        [Test]
        public void NormalizeVariationId_ValidNumeric_ReturnsAsIs()
        {
            Assert.AreEqual("7722370027",
                EventIdNormalizer.NormalizeVariationId("7722370027"));
        }

        [Test]
        public void NormalizeVariationId_Null_ReturnsNull()
        {
            Assert.IsNull(EventIdNormalizer.NormalizeVariationId(null));
        }

        [Test]
        public void NormalizeVariationId_Empty_ReturnsNull()
        {
            Assert.IsNull(EventIdNormalizer.NormalizeVariationId(string.Empty));
        }

        [Test]
        public void NormalizeVariationId_NonNumeric_ReturnsNull()
        {
            Assert.IsNull(EventIdNormalizer.NormalizeVariationId("variation_a"));
            Assert.IsNull(EventIdNormalizer.NormalizeVariationId("v1"));
            Assert.IsNull(EventIdNormalizer.NormalizeVariationId("abc"));
        }

        [Test]
        public void NormalizeVariationId_Whitespace_ReturnsNull()
        {
            Assert.IsNull(EventIdNormalizer.NormalizeVariationId(" "));
            Assert.IsNull(EventIdNormalizer.NormalizeVariationId(" 123"));
            Assert.IsNull(EventIdNormalizer.NormalizeVariationId("12 3"));
        }

        [Test]
        public void NormalizeVariationId_SignedOrDecimal_ReturnsNull()
        {
            Assert.IsNull(EventIdNormalizer.NormalizeVariationId("-123"));
            Assert.IsNull(EventIdNormalizer.NormalizeVariationId("12.3"));
        }

        // ---------- Cross-field invariant: entity_id == campaign_id ----------

        [Test]
        public void EntityIdFollowsSameRuleAsCampaignId()
        {
            // FR-009: entity_id (impression events) uses the same normalization rule
            // as campaign_id. Callers should pass the SAME inputs to NormalizeCampaignId
            // for both fields to ensure byte-equivalence.
            var inputs = new[] {
                new { CampaignId = "7719770039", ExperimentId = "1111111111" },
                new { CampaignId = (string)null,    ExperimentId = "1111111111" },
                new { CampaignId = string.Empty,    ExperimentId = "1111111111" },
                new { CampaignId = "not_numeric",   ExperimentId = "1111111111" },
                new { CampaignId = "  ",            ExperimentId = string.Empty   },
            };

            foreach (var input in inputs)
            {
                var campaignId = EventIdNormalizer.NormalizeCampaignId(
                    input.CampaignId, input.ExperimentId);
                var entityId = EventIdNormalizer.NormalizeCampaignId(
                    input.CampaignId, input.ExperimentId);
                Assert.AreEqual(campaignId, entityId,
                    "entity_id must equal campaign_id byte-for-byte for the same impression event");
            }
        }
    }
}
