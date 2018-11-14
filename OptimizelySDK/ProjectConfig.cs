/* 
 * Copyright 2017-2018, Optimizely
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
using Newtonsoft.Json;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Exceptions;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;
using System.Collections.Generic;
using Attribute = OptimizelySDK.Entity.Attribute;

namespace OptimizelySDK
{
    public class ProjectConfig
    {
        public enum OPTLYSDKVersion
        {
            V2 = 2,
            V3 = 3,
            V4 = 4
        }

        public const string RESERVED_ATTRIBUTE_PREFIX = "$opt_";

        /// <summary>
        /// Version of the datafile.
        /// </summary>
        public string Version { get; set; }


        /// <summary>
        /// Account ID of the account using the SDK.
        /// </summary>
        public string AccountId { get; set; }


        /// <summary>
        /// Project ID of the Full Stack project.
        /// </summary>
        public string ProjectId { get; set; }


        /// <summary>
        /// Revision of the dataflie.
        /// </summary>
        public string Revision { get; set; }

        /// <summary>
        /// Allow Anonymize IP by truncating the last block of visitors' IP address.
        /// </summary>
        public bool AnonymizeIP { get; set; }

        /// <summary>
        /// Bot filtering flag.
        /// </summary>
        public bool? BotFiltering { get; set; }


        private static List<OPTLYSDKVersion> SupportedVersions = new List<OPTLYSDKVersion> {
            OPTLYSDKVersion.V2,
            OPTLYSDKVersion.V3,
            OPTLYSDKVersion.V4
        };


        //========================= Mappings ===========================

        /// <summary>
        /// Associative array of group ID to Group(s) in the datafile
        /// </summary>
        private Dictionary<string, Group> _GroupIdMap;
        public Dictionary<string, Group> GroupIdMap { get { return _GroupIdMap; } }
        /// <summary>
        /// Associative array of experiment key to Experiment(s) in the datafile
        /// </summary>
        private Dictionary<string, Experiment> _ExperimentKeyMap;
        public Dictionary<string, Experiment> ExperimentKeyMap { get { return _ExperimentKeyMap; } }
        /// <summary>
        /// Associative array of experiment ID to Experiment(s) in the datafile
        /// </summary>
        private Dictionary<string, Experiment> _ExperimentIdMap 
            = new Dictionary<string, Experiment>();
        public Dictionary<string, Experiment> ExperimentIdMap { get { return _ExperimentIdMap; } }

        /// <summary>
        /// Associative array of experiment key to associative array of variation key to variations
        /// </summary>
        private Dictionary<string, Dictionary<string, Variation>> _VariationKeyMap 
            = new Dictionary<string, Dictionary<string, Variation>>();
        public Dictionary<string, Dictionary<string, Variation>> VariationKeyMap { get { return _VariationKeyMap; } }


        /// <summary>
        /// Associative array of experiment key to associative array of variation ID to variations
        /// </summary>
        private Dictionary<string, Dictionary<string, Variation>> _VariationIdMap 
            = new Dictionary<string, Dictionary<string, Variation>>();
        public Dictionary<string, Dictionary<string, Variation>> VariationIdMap { get { return _VariationIdMap; } }

        /// <summary>
        /// Associative array of event key to Event(s) in the datafile
        /// </summary>
        private Dictionary<string, Entity.Event> _EventKeyMap;
        public Dictionary<string, Entity.Event> EventKeyMap { get { return _EventKeyMap; } }

        /// <summary>
        /// Associative array of attribute key to Attribute(s) in the datafile
        /// </summary>
        private Dictionary<string, Attribute> _AttributeKeyMap;
        public Dictionary<string, Attribute> AttributeKeyMap { get { return _AttributeKeyMap; } }

        /// <summary>
        /// Associative array of audience ID to Audience(s) in the datafile
        /// </summary>
        private Dictionary<string, Audience> _AudienceIdMap;
        public Dictionary<string, Audience> AudienceIdMap { get { return _AudienceIdMap; } }

        /// <summary>
        /// Associative array of user IDs to an associative array
        /// of experiments to variations.This contains all the forced variations
        /// set by the user by calling setForcedVariation (it is not the same as the
        /// whitelisting forcedVariations data structure in the Experiments class).
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> _ForcedVariationMap;
        public Dictionary<string, Dictionary<string, string>> ForcedVariationMap { get { return _ForcedVariationMap; } }

        /// <summary>
        /// Associative array of Feature Key to Feature(s) in the datafile
        /// </summary>
        private Dictionary<string, FeatureFlag> _FeatureKeyMap;
        public Dictionary<string, FeatureFlag> FeatureKeyMap { get { return _FeatureKeyMap; } }

        /// <summary>
        /// Associative array of Rollout ID to Rollout(s) in the datafile
        /// </summary>
        private Dictionary<string, Rollout> _RolloutIdMap;
        public Dictionary<string, Rollout> RolloutIdMap { get { return _RolloutIdMap; } }
        

        //========================= Callbacks ===========================

        /// <summary>
        /// Logger for logging
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// ErrorHandler callback
        /// </summary>
        public IErrorHandler ErrorHandler { get; set; }


        //========================= Datafile Entities ===========================

        /// <summary>
        /// Associative list of groups to Group(s) in the datafile
        /// </summary>
        public Group[] Groups { get; set; }

        /// <summary>
        /// Associative list of experiments to Experiment(s) in the datafile.
        /// </summary>
        public Experiment[] Experiments { get; set; }


        /// <summary>
        /// Associative list of events.
        /// </summary>
        public Entity.Event[] Events { get; set; }

        /// <summary>
        /// Associative list of Attributes.
        /// </summary>
        public Attribute[] Attributes { get; set; }
    
        /// <summary>
        /// Associative list of Audiences.
        /// </summary>
        public Audience[] Audiences { get; set; }

        /// <summary>
        /// Associative list of FeatureFlags.
        /// </summary>
        public FeatureFlag[] FeatureFlags { get; set; }

        /// <summary>
        /// Associative list of Rollouts.
        /// </summary>
        public Rollout[] Rollouts { get; set; }

        //========================= Initialization ===========================

        /// <summary>
        /// Constructor (private)
        /// </summary>
        private ProjectConfig()
        {
        }

        /// <summary>
        /// Initialize the arrays and mappings
        /// This can't be done in the constructor because the object is created via serialization
        /// </summary>
        private void Initialize()
        {
            Groups = Groups ?? new Group[0];
            Experiments = Experiments ?? new Experiment[0];
            Events = Events ?? new Entity.Event[0];
            Attributes = Attributes ?? new Attribute[0];
            Audiences = Audiences ?? new Audience[0];
            FeatureFlags = FeatureFlags ?? new FeatureFlag[0];
            Rollouts = Rollouts ?? new Rollout[0];

            _ForcedVariationMap = new Dictionary<string, Dictionary<string, string>>();
            _GroupIdMap = ConfigParser<Group>.GenerateMap(entities: Groups, getKey: g => g.Id.ToString(), clone: true);
            _ExperimentKeyMap = ConfigParser<Experiment>.GenerateMap(entities: Experiments, getKey: e => e.Key, clone: true);
            _EventKeyMap = ConfigParser<Entity.Event>.GenerateMap(entities: Events, getKey: e => e.Key, clone: true);
            _AttributeKeyMap = ConfigParser<Attribute>.GenerateMap(entities: Attributes, getKey: a => a.Key, clone: true);
            _AudienceIdMap = ConfigParser<Audience>.GenerateMap(entities: Audiences, getKey: a => a.Id.ToString(), clone: true);
            _FeatureKeyMap = ConfigParser<FeatureFlag>.GenerateMap(entities: FeatureFlags, getKey: f => f.Key, clone: true);
            _RolloutIdMap = ConfigParser<Rollout>.GenerateMap(entities: Rollouts, getKey: r => r.Id.ToString(), clone: true);

            foreach (Group group in Groups)
            {
                var experimentsInGroup = ConfigParser<Experiment>.GenerateMap(group.Experiments, getKey: e => e.Key, clone: true);
                foreach (Experiment experiment in experimentsInGroup.Values)
                {
                    experiment.GroupId = group.Id;
                    experiment.GroupPolicy = group.Policy;
                }
                
                // RJE: I believe that this is equivalent to this:
                // $this->_experimentKeyMap = array_merge($this->_experimentKeyMap, $experimentsInGroup);
                foreach (string key in experimentsInGroup.Keys)
                    _ExperimentKeyMap[key] = experimentsInGroup[key];
            }

            foreach (Experiment experiment in _ExperimentKeyMap.Values)
            {
                _VariationKeyMap[experiment.Key] = new Dictionary<string, Variation>();
                _VariationIdMap[experiment.Key] = new Dictionary<string, Variation>();
                _ExperimentIdMap[experiment.Id] = experiment;

                if (experiment.Variations != null)
                {
                    foreach (Variation variation in experiment.Variations)
                    {
                        _VariationKeyMap[experiment.Key][variation.Key] = variation;
                        _VariationIdMap[experiment.Key][variation.Id] = variation;
                    }
                }
            }

            // Adding Rollout variations in variation id and key maps.
            foreach (var rollout in Rollouts)
            {
                foreach (var rolloutRule in rollout.Experiments)
                {
                    _VariationKeyMap[rolloutRule.Key] = new Dictionary<string, Variation>();
                    _VariationIdMap[rolloutRule.Key] = new Dictionary<string, Variation>();
                    
                    if (rolloutRule.Variations != null)
                    {
                        foreach (var variation in rolloutRule.Variations)
                        {
                            _VariationKeyMap[rolloutRule.Key][variation.Key] = variation;
                            _VariationIdMap[rolloutRule.Key][variation.Id] = variation;
                        }
                    }
                }
            }
        }

        public static ProjectConfig Create(string content, ILogger logger, IErrorHandler errorHandler)
        {
            ProjectConfig config = GetConfig(content);

            config.Logger = logger;
            config.ErrorHandler = errorHandler;

            config.Initialize();

            return config;
        }

        private static ProjectConfig GetConfig(string configData)
        {
            if (configData == null)
                throw new ConfigParseException("Unable to parse null datafile.");

            if (string.IsNullOrEmpty(configData))
                throw new ConfigParseException("Unable to parse empty datafile.");

            var config = JsonConvert.DeserializeObject<ProjectConfig>(configData);

            if (SupportedVersions.TrueForAll((supportedVersion) => !(((int)supportedVersion).ToString() == config.Version)))
                throw new ConfigParseException(string.Format(@"This version of the C# SDK does not support the given datafile version: {0}", config.Version));

            return config;
        }

        //========================= Getters ===========================

        /// <summary>
        /// Get the group associated with groupId
        /// </summary>
        /// <param name="groupId">string ID of the group</param>
        /// <returns>Group Entity corresponding to the ID or a dummy entity if groupId is invalid</returns>
        public Group GetGroup(string groupId)
        {
            if (_GroupIdMap.ContainsKey(groupId))
                return _GroupIdMap[groupId];

            string message = string.Format(@"Group ID ""{0}"" is not in datafile.", groupId);
            Logger.Log(LogLevel.ERROR, message);
            ErrorHandler.HandleError(new Exceptions.InvalidGroupException("Provided group is not in datafile."));
            return new Group();
        }

        /// <summary>
        /// Get the experiment from the key
        /// </summary>
        /// <param name="experimentKey">Key of the experiment</param>
        /// <returns>Experiment Entity corresponding to the key or a dummy entity if key is invalid</returns>
        public Experiment GetExperimentFromKey(string experimentKey)
        {
            if (_ExperimentKeyMap.ContainsKey(experimentKey))
                return _ExperimentKeyMap[experimentKey];

            string message = string.Format(@"Experiment key ""{0}"" is not in datafile.", experimentKey);
            Logger.Log(LogLevel.ERROR, message);
            ErrorHandler.HandleError(new Exceptions.InvalidExperimentException("Provided experiment is not in datafile."));
            return new Experiment();
        }

        /// <summary>
        /// Get the experiment from the ID
        /// </summary>
        /// <param name="experimentId">ID of the experiment</param>
        /// <returns>Experiment Entity corresponding to the IDkey or a dummy entity if ID is invalid</returns>
        public Experiment GetExperimentFromId(string experimentId)
        {
            if (_ExperimentIdMap.ContainsKey(experimentId))
                return _ExperimentIdMap[experimentId];

            string message = string.Format(@"Experiment ID ""{0}"" is not in datafile.", experimentId);
            Logger.Log(LogLevel.ERROR, message);
            ErrorHandler.HandleError(new Exceptions.InvalidExperimentException("Provided experiment is not in datafile."));
            return new Experiment();
        }

        /// <summary>
        /// Get the Event from the key
        /// </summary>
        /// <param name="eventKey">Key of the event</param>
        /// <returns>Event Entity corresponding to the key or a dummy entity if key is invalid</returns>
        public Entity.Event GetEvent(string eventKey)
        {
            if (_EventKeyMap.ContainsKey(eventKey))
                return _EventKeyMap[eventKey];

            string message = string.Format(@"Event key ""{0}"" is not in datafile.", eventKey);
            Logger.Log(LogLevel.ERROR, message);
            ErrorHandler.HandleError(new Exceptions.InvalidEventException("Provided event is not in datafile."));
            return new Entity.Event();
        }

        /// <summary>
        /// Get the Audience from the ID
        /// </summary>
        /// <param name="audienceId">ID of the Audience</param>
        /// <returns>Audience Entity corresponding to the ID or a dummy entity if ID is invalid</returns>
        public Audience GetAudience(string audienceId)
        {
            if (_AudienceIdMap.ContainsKey(audienceId))
                return _AudienceIdMap[audienceId];

            string message = string.Format(@"Audience ID ""{0}"" is not in datafile.", audienceId);
            Logger.Log(LogLevel.ERROR, message);
            ErrorHandler.HandleError(new Exceptions.InvalidAudienceException("Provided audience is not in datafile."));
            return new Audience();
        }

        /// <summary>
        /// Get the Attribute from the key
        /// </summary>
        /// <param name="attributeKey">Key of the Attribute</param>
        /// <returns>Attribute Entity corresponding to the key or a dummy entity if key is invalid</returns>
        public Attribute GetAttribute(string attributeKey)
        {
            if (_AttributeKeyMap.ContainsKey(attributeKey))
                return _AttributeKeyMap[attributeKey];

            string message = string.Format(@"Attribute key ""{0}"" is not in datafile.", attributeKey);
            Logger.Log(LogLevel.ERROR, message);
            ErrorHandler.HandleError(new Exceptions.InvalidAttributeException("Provided attribute is not in datafile."));
            return new Attribute();
        }

        /// <summary>
        /// Get the Variation from the keys
        /// </summary>
        /// <param name="experimentKey">key for Experiment</param>
        /// <param name="variationKey">key for Variation</param>
        /// <returns>Variation Entity corresponding to the provided experiment key and variation key or a dummy 
        /// entity if keys are invalid</returns>
        public Variation GetVariationFromKey(string experimentKey, string variationKey)
        {
            if (_VariationKeyMap.ContainsKey(experimentKey) &&
                _VariationKeyMap[experimentKey].ContainsKey(variationKey))
                return _VariationKeyMap[experimentKey][variationKey];

            string message = string.Format(@"No variation key ""{0}"" defined in datafile for experiment ""{1}"".", 
                variationKey, experimentKey);
            Logger.Log(LogLevel.ERROR, message);
            ErrorHandler.HandleError(new Exceptions.InvalidVariationException("Provided variation is not in datafile."));
            return new Variation();
        }

        /// <summary>
        /// Get the Variation from the Key/ID
        /// </summary>
        /// <param name="experimentKey">key for Experiment</param>
        /// <param name="variationId">ID for Variation</param>
        /// <returns>Variation Entity corresponding to the provided experiment key and variation ID or a dummy 
        /// entity if key or ID is invalid</returns>
        public Variation GetVariationFromId(string experimentKey, string variationId)
        {
            if (_VariationIdMap.ContainsKey(experimentKey) &&
                _VariationIdMap[experimentKey].ContainsKey(variationId))
                return _VariationIdMap[experimentKey][variationId];

            string message = string.Format(@"No variation ID ""{0}"" defined in datafile for experiment ""{1}"".",
                variationId, experimentKey);
            Logger.Log(LogLevel.ERROR, message);
            ErrorHandler.HandleError(new Exceptions.InvalidVariationException("Provided variation is not in datafile."));
            return new Variation();
        }

        /// <summary>
        /// Gets the forced variation for the given user and experiment.  
        /// </summary>
        /// <param name="experimentKey">The experiment key</param>
        /// <param name="userId">The user ID</param>
        /// <returns>Variation entity which the given user and experiment should be forced into.</returns>
        public Variation GetForcedVariation(string experimentKey, string userId)
        {
            if (_ForcedVariationMap.ContainsKey(userId) == false)
            {
                Logger.Log(LogLevel.DEBUG, string.Format(@"User ""{0}"" is not in the forced variation map.", userId));
                return null;
            }

            Dictionary<string, string> experimentToVariationMap = _ForcedVariationMap[userId];

            string experimentId = GetExperimentFromKey(experimentKey).Id;

            // this case is logged in getExperimentFromKey
            if (string.IsNullOrEmpty(experimentId))
                return null;
            
            if (experimentToVariationMap.ContainsKey(experimentId) == false)
            {
                Logger.Log(LogLevel.DEBUG, string.Format(@"No experiment ""{0}"" mapped to user ""{1}"" in the forced variation map.", experimentKey, userId));
                return null;
            }
            
            string variationId = experimentToVariationMap[experimentId];

            if (string.IsNullOrEmpty(variationId))
            {
                Logger.Log(LogLevel.DEBUG, string.Format(@"No variation mapped to experiment ""{0}"" in the forced variation map.", experimentKey));
                return null;
            }
            
            string variationKey = GetVariationFromId(experimentKey, variationId).Key;

            // this case is logged in getVariationFromKey
            if (string.IsNullOrEmpty(variationKey))
                return null;

            Logger.Log(LogLevel.DEBUG, string.Format(@"Variation ""{0}"" is mapped to experiment ""{1}"" and user ""{2}"" in the forced variation map", variationKey, experimentKey, userId));
            
            Variation variation = GetVariationFromKey(experimentKey, variationKey);

            return variation;
        }

        /// <summary>
        /// Sets an associative array of user IDs to an associative array of experiments to forced variations.
        /// </summary>
        /// <param name="experimentKey">The experiment key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="variationKey">The variation key</param>
        /// <returns>A boolean value that indicates if the set completed successfully.</returns>
        public bool SetForcedVariation(string experimentKey, string userId, string variationKey)
        {
            // Empty variation key is considered as invalid.
            if (variationKey != null && variationKey.Length == 0)
            {
                Logger.Log(LogLevel.DEBUG, "Variation key is invalid.");
                return false;
            }

            var experimentId = GetExperimentFromKey(experimentKey).Id;

            // this case is logged in getExperimentFromKey
            if (string.IsNullOrEmpty(experimentId))
                return false;

            // clear the forced variation if the variation key is null
            if (variationKey == null)
            {
                if (_ForcedVariationMap.ContainsKey(userId) && _ForcedVariationMap[userId].ContainsKey(experimentId))
                    _ForcedVariationMap[userId].Remove(experimentId);

                Logger.Log(LogLevel.DEBUG, string.Format(@"Variation mapped to experiment ""{0}"" has been removed for user ""{1}"".", experimentKey, userId));
                return true;
            }
            
            string variationId = GetVariationFromKey(experimentKey, variationKey).Id;

            // this case is logged in getVariationFromKey
            if (string.IsNullOrEmpty(variationId))
                return false;

            // Add User if not exist.
            if (_ForcedVariationMap.ContainsKey(userId) == false)
                _ForcedVariationMap[userId] = new Dictionary<string, string>();

            // Add/Replace Experiment to Variation ID map.
            _ForcedVariationMap[userId][experimentId] = variationId;

            Logger.Log(LogLevel.DEBUG, string.Format(@"Set variation ""{0}"" for experiment ""{1}"" and user ""{2}"" in the forced variation map.", variationId, experimentId, userId));
            return true;
        }

        /// <summary>
        /// Get the feature from the key
        /// </summary>
        /// <param name="featureKey">Key of the feature</param>
        /// <returns>Feature Flag Entity corresponding to the key or a dummy entity if key is invalid</returns>
        public FeatureFlag GetFeatureFlagFromKey(string featureKey)
        {
            if (_FeatureKeyMap.ContainsKey(featureKey))
                return _FeatureKeyMap[featureKey];

            string message = string.Format(@"Feature key ""{0}"" is not in datafile.", featureKey);
            Logger.Log(LogLevel.ERROR, message);
            ErrorHandler.HandleError(new Exceptions.InvalidFeatureException("Provided feature is not in datafile."));
            return new FeatureFlag();
        }

        /// <summary>
        /// Get the rollout from the ID
        /// </summary>
        /// <param name="rolloutId">ID for rollout</param>
        /// <returns>Rollout Entity corresponding to the rollout ID or a dummy entity if ID is invalid</returns>
        public Rollout GetRolloutFromId(string rolloutId)
        {
            if (_RolloutIdMap.ContainsKey(rolloutId))
                return _RolloutIdMap[rolloutId];

            string message = string.Format(@"Rollout ID ""{0}"" is not in datafile.", rolloutId);
            Logger.Log(LogLevel.ERROR, message);
            ErrorHandler.HandleError(new Exceptions.InvalidRolloutException("Provided rollout is not in datafile."));
            return new Rollout();
        }

        /// <summary>
        /// Get attribute ID for the provided attribute key
        /// </summary>
        /// <param name="attributeKey">Key of the Attribute</param>
        /// <returns>Attribute ID corresponding to the provided attribute key. Attribute key if it is a reserved attribute</returns>
        public string GetAttributeId(string attributeKey)
        {
            
            var hasReservedPrefix = attributeKey.StartsWith(RESERVED_ATTRIBUTE_PREFIX);

            if (_AttributeKeyMap.ContainsKey(attributeKey))
            {
                var attribute = _AttributeKeyMap[attributeKey];
                if (hasReservedPrefix)
                    Logger.Log(LogLevel.WARN, $@"Attribute {attributeKey} unexpectedly has reserved prefix {RESERVED_ATTRIBUTE_PREFIX}; using attribute ID instead of reserved attribute name.");

                return attribute.Id;
            }
            else if (hasReservedPrefix)
            {
                return attributeKey;
            }

            Logger.Log(LogLevel.ERROR, $@"Attribute key ""{attributeKey}"" is not in datafile.");
            return null;
        }
    }
}
