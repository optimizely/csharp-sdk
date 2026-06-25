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

namespace OptimizelySDK.Utils
{
    /// <summary>
    /// Normalizes decision-event identifier fields (campaign_id, variation_id, entity_id)
    /// so that the wire output is byte-equivalent across SDKs for the same input
    /// regardless of decision type (experiment, feature test, rollout, holdout).
    ///
    /// Two distinct validity definitions apply:
    ///
    /// - "Non-empty string" (campaign_id, impression-event entity_id): a string value of
    ///   length >= 1 with any character content. Numeric ("12345"), prefixed
    ///   ("default-12345"), and opaque ("layer_abc") IDs are all valid. Only `null` and
    ///   the empty string `""` trigger the fallback. Whitespace-only strings (e.g. " ")
    ///   are non-empty strings and therefore PASS THROUGH unchanged per the relaxed
    ///   contract (the upstream datafile is expected to deliver well-formed string IDs).
    ///
    /// - "Numeric string" (variation_id only): a non-empty string consisting entirely of
    ///   decimal digits [0-9]. Leading zeros are allowed. Whitespace, signs, decimals
    ///   and exponents are INVALID and trigger the null fallback.
    ///
    /// Rules:
    /// - campaign_id (and impression-event entity_id): if not a non-empty string,
    ///   substitute the provided experiment_id (which may itself be empty or null;
    ///   callers MUST normalize experiment_id separately if they want a guarantee here).
    /// - variation_id: if not a non-empty numeric string, substitute null.
    ///
    /// Non-string types (raw number, boolean, object) are out of scope per the spec
    /// — the upstream datafile producer is assumed to deliver string-typed (or null)
    /// values for these three fields.
    ///
    /// This normalization MUST NOT log, warn, throw, drop, or defer event dispatch.
    /// </summary>
    internal static class EventIdNormalizer
    {
        /// <summary>
        /// Returns true if <paramref name="value"/> is a non-empty string (length >= 1).
        /// Any character content is accepted — IDs may be opaque like "default-12345"
        /// or "layer_abc". Only `null` and the empty string return false.
        /// </summary>
        internal static bool IsNonEmptyString(string value)
        {
            return value != null && value.Length > 0;
        }

        /// <summary>
        /// Returns true if <paramref name="value"/> is a non-empty string consisting
        /// entirely of decimal digits [0-9]. Leading zeros are allowed.
        /// </summary>
        internal static bool IsNumericIdString(string value)
        {
            if (value == null)
            {
                return false;
            }

            if (value.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Normalize a campaign_id (or impression-event entity_id, which follows the same rule).
        /// If <paramref name="campaignId"/> is a non-empty string, return it unchanged
        /// (any character content is accepted — numeric, prefixed, or opaque).
        /// Otherwise (null or empty string) substitute <paramref name="experimentId"/>.
        /// The returned value is returned as-is (NOT recursively normalized) so callers
        /// see the exact substitute they passed in, matching the cross-SDK contract.
        /// </summary>
        internal static string NormalizeCampaignId(string campaignId, string experimentId)
        {
            return IsNonEmptyString(campaignId) ? campaignId : experimentId;
        }

        /// <summary>
        /// Normalize a variation_id. If <paramref name="variationId"/> is a valid numeric
        /// string, return it unchanged. Otherwise return null.
        /// </summary>
        internal static string NormalizeVariationId(string variationId)
        {
            return IsNumericIdString(variationId) ? variationId : null;
        }
    }
}
