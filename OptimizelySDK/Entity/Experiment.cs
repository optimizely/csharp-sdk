/* 
 * Copyright 2017-2019, Optimizely
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
using Newtonsoft.Json.Linq;
using OptimizelySDK.Utils;
using OptimizelySDK.AudienceConditions;
using Newtonsoft.Json;

namespace OptimizelySDK.Entity
{
    public class Experiment : IdKeyEntity
    {
        const string STATUS_RUNNING = "Running";

        const string MUTEX_GROUP_POLICY = "random";

        /// <summary>
        /// Experiment Status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Layer ID for the experiment
        /// </summary>
        public string LayerId { get; set; }

        /// <summary>
        /// Group ID for the experiment
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// Variations for the experiment
        /// </summary>
        public Variation[] Variations { get; set; }

        /// <summary>
        /// ForcedVariations for the experiment
        /// </summary>
        public Dictionary<string, string> ForcedVariations { get; set; }

		/// <summary>
		/// ForcedVariations for the experiment
		/// </summary>
       	public Dictionary<string, string> UserIdToKeyVariations { get { return ForcedVariations; } }

        /// <summary>
        /// Policy of the experiment group
        /// </summary>
        public string GroupPolicy { get; set; }

        /// <summary>
        /// ID(s) of audience(s) the experiment is targeted to
        /// </summary>
        public string[] AudienceIds { get; set; }

        private ICondition _audienceIdsList = null;

        /// <summary>
        /// De-serialized audience conditions
        /// </summary>
        public ICondition AudienceIdsList
        {
            get
            {
                if (AudienceIds == null || AudienceIds.Length == 0)
                    return null;
                
                if (_audienceIdsList == null)
                {
                    var conditions = new List<ICondition>();
                    foreach (var audienceId in AudienceIds)
                        conditions.Add(new AudienceIdCondition() { AudienceId = (string)audienceId });

                    _audienceIdsList = new OrCondition() { Conditions = conditions.ToArray() };
                }
                
                return _audienceIdsList;
            }
        }

        private string _audienceIdsString = null;

        /// <summary>
        /// Stringified audience conditions
        /// </summary>
        public string AudienceIdsString
        {
            get
            {
                if (AudienceIds == null)
                    return null;

                if (_audienceIdsString == null)
                    _audienceIdsString = JsonConvert.SerializeObject(AudienceIds, Formatting.None);

                return _audienceIdsString;
            }
        }

        /// <summary>
        /// Traffic allocation of variations in the experiment
        /// </summary>
        public TrafficAllocation[] TrafficAllocation { get; set; }

        /// <summary>
        /// Audience Conditions
        /// </summary>
        public object AudienceConditions { get; set; }

        private ICondition _audienceConditionsList = null;

        /// <summary>
        /// De-serialized audience conditions
        /// </summary>
        public ICondition AudienceConditionsList
        {
            get
            {
                if (AudienceConditions == null)
                    return null;

                if (_audienceConditionsList == null)
                {
                    if (AudienceConditions is string)
                        _audienceConditionsList = ConditionParser.ParseAudienceConditions(JToken.Parse((string)AudienceConditions));
                    else
                        _audienceConditionsList = ConditionParser.ParseAudienceConditions((JToken)AudienceConditions);
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
                    return null;

                if (_audienceConditionsString == null)
                {
                    if (AudienceConditions is JToken token)
                        _audienceConditionsString = token.ToString(Formatting.None);
                    else
                        _audienceConditionsString = AudienceConditions.ToString();
                }

                return _audienceConditionsString;
            }
        }

        bool isGenerateKeyMapCalled = false;

        private Dictionary<string, Variation> _VariationKeyToVariationMap;
        public Dictionary<string, Variation> VariationKeyToVariationMap {
            get {
                if (!isGenerateKeyMapCalled) GenerateVariationKeyMap();
                return _VariationKeyToVariationMap;
            }
        }

		private Dictionary<string, Variation> _VariationIdToVariationMap;
		public Dictionary<string, Variation> VariationIdToVariationMap {
            get
            {
                if (!isGenerateKeyMapCalled) GenerateVariationKeyMap();
                return _VariationIdToVariationMap;
            }
        }

        public void GenerateVariationKeyMap()
        {
            if (Variations == null) return;
            _VariationIdToVariationMap = ConfigParser<Variation>.GenerateMap(entities: Variations, getKey: a => a.Id, clone: true);
            _VariationKeyToVariationMap = ConfigParser<Variation>.GenerateMap(entities: Variations, getKey: a => a.Key, clone: true);
            isGenerateKeyMapCalled = true;
        }

		// Code from PHP, need to build traffic and variations from config
#if false
        /**
         * @param $variations array Variations in experiment.
         */
        public function setVariations($variations)
        {
        $this->_variations = ConfigParser::generateMap($variations, null, Variation::class);
        }

        /**
         * @param $trafficAllocation array Traffic allocation of variations in experiment.
         */
        public function setTrafficAllocation($trafficAllocation)
        {
        $this->_trafficAllocation = ConfigParser::generateMap($trafficAllocation, null, TrafficAllocation::class);
        }
#endif

		/// <summary>
		/// Determine if experiment is in a mutually exclusive group
		/// </summary>
		public bool IsInMutexGroup
        {
            get
            {
                return !string.IsNullOrEmpty(GroupPolicy) && GroupPolicy == MUTEX_GROUP_POLICY;
            }
        }

        /// <summary>
        /// Determine if experiment is running or not
        /// </summary>
        public bool IsExperimentRunning
        {
            get
            {
                return !string.IsNullOrEmpty(Status) && Status == STATUS_RUNNING;
            }
        }

        /// <summary>
        /// Determin if user is forced variation of experiment
        /// </summary>
        /// <param name="userId">User ID of the user</param>
        /// <returns>True iff user is in forced variation of experiment</returns>
        public bool IsUserInForcedVariation(string userId)
        {
            return ForcedVariations != null && ForcedVariations.ContainsKey(userId);
        }
    }
}
