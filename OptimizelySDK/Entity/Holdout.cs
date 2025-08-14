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

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Entity
{
    /// <summary>
    /// Represents a holdout in an Optimizely project
    /// </summary>
    public class Holdout : IdKeyEntity, IExperimentCore
    {
        /// <summary>
        /// Constructor that initializes properties to avoid null values
        /// </summary>
        public Holdout()
        {
            Id = "";
            Key = "";
        }

        /// <summary>
        /// Holdout status enumeration
        /// </summary>
        public enum HoldoutStatus
        {
            Draft,
            Running,
            Concluded,
            Archived
        }

        private const string STATUS_RUNNING = "Running";

        /// <summary>
        /// Holdout Status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Layer ID for the holdout
        /// </summary>
        public string LayerId { get; set; }

        /// <summary>
        /// Variations for the holdout
        /// </summary>
        public Variation[] Variations { get; set; }

        /// <summary>
        /// Traffic allocation of variations in the holdout
        /// </summary>
        public TrafficAllocation[] TrafficAllocation { get; set; }

        /// <summary>
        /// ID(s) of audience(s) the holdout is targeted to
        /// </summary>
        public string[] AudienceIds { get; set; }

        /// <summary>
        /// Audience Conditions
        /// </summary>
        public object AudienceConditions { get; set; }

        /// <summary>
        /// Flags included in this holdout
        /// </summary>
        public string[] IncludedFlags { get; set; } = new string[0];

        /// <summary>
        /// Flags excluded from this holdout
        /// </summary>
        public string[] ExcludedFlags { get; set; } = new string[0];

        #region Audience Processing Properties

        private ICondition _audienceIdsList = null;

        /// <summary>
        /// De-serialized audience conditions from audience IDs
        /// </summary>
        public ICondition AudienceIdsList
        {
            get
            {
                if (AudienceIds == null || AudienceIds.Length == 0)
                {
                    return null;
                }

                if (_audienceIdsList == null)
                {
                    var conditions = new List<ICondition>();
                    foreach (var audienceId in AudienceIds)
                    {
                        conditions.Add(new AudienceIdCondition() { AudienceId = audienceId });
                    }

                    _audienceIdsList = new OrCondition() { Conditions = conditions.ToArray() };
                }

                return _audienceIdsList;
            }
        }

        private string _audienceIdsString = null;

        /// <summary>
        /// Stringified audience IDs
        /// </summary>
        public string AudienceIdsString
        {
            get
            {
                if (AudienceIds == null)
                {
                    return null;
                }

                if (_audienceIdsString == null)
                {
                    _audienceIdsString = JsonConvert.SerializeObject(AudienceIds, Formatting.None);
                }

                return _audienceIdsString;
            }
        }

        private ICondition _audienceConditionsList = null;

        /// <summary>
        /// De-serialized audience conditions
        /// </summary>
        public ICondition AudienceConditionsList
        {
            get
            {
                if (AudienceConditions == null)
                {
                    return null;
                }

                if (_audienceConditionsList == null)
                {
                    if (AudienceConditions is string)
                    {
                        _audienceConditionsList =
                            ConditionParser.ParseAudienceConditions(
                                JToken.Parse((string)AudienceConditions));
                    }
                    else
                    {
                        _audienceConditionsList =
                            ConditionParser.ParseAudienceConditions((JToken)AudienceConditions);
                    }
                }

                return _audienceConditionsList;
            }
        }

        private string _audienceConditionsString = null;

        /// <summary>
        /// Stringified audience conditions
        /// </summary>
        public string AudienceConditionsString
        {
            get
            {
                if (AudienceConditions == null)
                {
                    return null;
                }

                if (_audienceConditionsString == null)
                {
                    if (AudienceConditions is JToken token)
                    {
                        _audienceConditionsString = token.ToString(Formatting.None);
                    }
                    else
                    {
                        _audienceConditionsString = AudienceConditions.ToString();
                    }
                }

                return _audienceConditionsString;
            }
        }

        #endregion

        #region Variation Mapping Properties

        private bool isGenerateKeyMapCalled = false;

        private Dictionary<string, Variation> _VariationKeyToVariationMap;

        /// <summary>
        /// Variation key to variation mapping
        /// </summary>
        public Dictionary<string, Variation> VariationKeyToVariationMap
        {
            get
            {
                if (!isGenerateKeyMapCalled)
                {
                    GenerateVariationKeyMap();
                }

                return _VariationKeyToVariationMap;
            }
        }

        private Dictionary<string, Variation> _VariationIdToVariationMap;

        /// <summary>
        /// Variation ID to variation mapping
        /// </summary>
        public Dictionary<string, Variation> VariationIdToVariationMap
        {
            get
            {
                if (!isGenerateKeyMapCalled)
                {
                    GenerateVariationKeyMap();
                }

                return _VariationIdToVariationMap;
            }
        }

        /// <summary>
        /// Generate variation key maps for performance optimization
        /// </summary>
        public void GenerateVariationKeyMap()
        {
            if (Variations == null)
            {
                return;
            }

            _VariationIdToVariationMap =
                ConfigParser<Variation>.GenerateMap(Variations, a => a.Id, true);
            _VariationKeyToVariationMap =
                ConfigParser<Variation>.GenerateMap(Variations, a => a.Key, true);
            isGenerateKeyMapCalled = true;
        }

        #endregion

        /// <summary>
        /// Determine if holdout is currently activated/running
        /// </summary>
        public bool IsActivated =>
            !string.IsNullOrEmpty(Status) && Status == STATUS_RUNNING;

        /// <summary>
        /// Serializes audiences with provided audience map for display purposes
        /// </summary>
        /// <param name="audiencesMap">Map of audience ID to audience name</param>
        /// <returns>Serialized audience string with names</returns>
        public string SerializeAudiences(Dictionary<string, string> audiencesMap)
        {
            if (AudienceConditions == null)
            {
                return string.Empty;
            }

            var serialized = AudienceConditionsString;
            return this.ReplaceAudienceIdsWithNames(serialized, audiencesMap);
        }
    }
}
