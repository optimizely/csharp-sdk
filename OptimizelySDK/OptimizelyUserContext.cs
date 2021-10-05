﻿/* 
 * Copyright 2020-2021, Optimizely
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

using OptimizelySDK.Logger;
using System.Collections.Generic;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Entity;
using OptimizelySDK.OptimizelyDecisions;

namespace OptimizelySDK
{
    /// <summary>
    /// OptimizelyUserContext defines user contexts that the SDK will use to make decisions for
    /// </summary>
    public class OptimizelyUserContext
    {
        private class ForcedDecision
        {
            private string flagKey;
            private string ruleKey;
            private string variationKey;

            public ForcedDecision(string flagKey, string ruleKey, string variationKey)
            {
                this.flagKey = flagKey;
                this.ruleKey = ruleKey;
                this.variationKey = variationKey;
            }

            public string FlagKey { get { return flagKey; } set { this.flagKey = value; } }

            public string RuleKey { get { return ruleKey; } set { this.ruleKey = value; } }

            public string VariationKey { get { return variationKey; } set { this.variationKey = value; } }
        }

        private ILogger Logger;
        private IErrorHandler ErrorHandler;
        private object mutex = new object();
        // userID for Optimizely user context
        private string UserId;
        // user attributes for Optimizely user context.
        private UserAttributes Attributes;
        // Optimizely object to be used.
        private Optimizely Optimizely;

        private Dictionary<string, Dictionary<string, ForcedDecision>> ForcedDecisionsMap = new Dictionary<string, Dictionary<string, ForcedDecision>>();
        private Dictionary<string, ForcedDecision> ForcedDecisionsMapWithNoRuleKey = new Dictionary<string, ForcedDecision>();

        public OptimizelyUserContext(Optimizely optimizely, string userId, UserAttributes userAttributes, IErrorHandler errorHandler, ILogger logger)
        {
            ErrorHandler = errorHandler;
            Logger = logger;
            Optimizely = optimizely;
            Attributes = userAttributes ?? new UserAttributes();
            UserId = userId;
        }

        private OptimizelyUserContext Copy() => new OptimizelyUserContext(Optimizely, UserId, GetAttributes(), ErrorHandler, Logger);

        /// <summary>
        /// Returns Optimizely instance associated with the UserContext.
        /// </summary>
        /// <returns> Optimizely instance.</returns>
        public virtual Optimizely GetOptimizely()
        {
            return Optimizely;
        }

        /// <summary>
        /// Returns UserId associated with the UserContext
        /// </summary>
        /// <returns>UserId of this instance.</returns>
        public virtual string GetUserId()
        {
            return UserId;
        }

        /// <summary>
        /// Returns copy of UserAttributes associated with UserContext.
        /// </summary>
        /// <returns>copy of UserAttributes.</returns>
        public UserAttributes GetAttributes()
        {
            UserAttributes copiedAttributes = null;
            lock(mutex) {
                copiedAttributes = new UserAttributes(Attributes);
            }

            return copiedAttributes;
        }

        /// <summary>
        /// Set an attribute for a given key.
        /// </summary>
        /// <param name="key">An attribute key</param>
        /// <param name="value">value An attribute value</param>
        public void SetAttribute(string key, object value)
        {
            if (key == null)
            {
                Logger.Log(LogLevel.WARN, "Null attribute key.");
            }
            else
            {
                lock (mutex)
                {
                    Attributes[key] = value;
                }
            }
        }

        /// <summary>
        /// Returns a decision result ({@link OptimizelyDecision}) for a given flag key and a user context, which contains all data required to deliver the flag.
        /// <ul>
        /// <li>If the SDK finds an error, it’ll return a decision with <b>null</b> for <b>variationKey</b>. The decision will include an error message in <b>reasons</b>.
        /// </ul>
        /// </summary>
        /// <param name="key">A flag key for which a decision will be made.</param>
        /// <returns>A decision result.</returns>
        public virtual OptimizelyDecision Decide(string key)
        {
            return Decide(key, new OptimizelyDecideOption[] { });
        }

        /// <summary>
        /// Returns a decision result ({@link OptimizelyDecision}) for a given flag key and a user context, which contains all data required to deliver the flag.
        /// <ul>
        /// <li>If the SDK finds an error, it’ll return a decision with <b>null</b> for <b>variationKey</b>. The decision will include an error message in <b>reasons</b>.
        /// </ul>
        /// </summary>
        /// <param name="key">A flag key for which a decision will be made.</param>
        /// <param name="options">A list of options for decision-making.</param>
        /// <returns>A decision result.</returns>
        public virtual OptimizelyDecision Decide(string key,
            OptimizelyDecideOption[] options)
        {
            var optimizelyUserContext = Copy();
            return Optimizely.Decide(optimizelyUserContext, key, options);
        }

        /// <summary>
        /// Returns a key-map of decision results for multiple flag keys and a user context.
        /// </summary>
        /// <param name="keys">list of flag keys for which a decision will be made.</param>
        /// <returns>A dictionary of all decision results, mapped by flag keys.</returns>
        public virtual Dictionary<string, OptimizelyDecision> DecideForKeys(string[] keys, OptimizelyDecideOption[] options)
        {
            var optimizelyUserContext = Copy();
            return Optimizely.DecideForKeys(optimizelyUserContext, keys, options);
        }

        /// <summary>
        /// Returns a key-map of decision results for multiple flag keys and a user context.
        /// </summary>
        /// <param name="keys">list of flag keys for which a decision will be made.</param>
        /// <returns>A dictionary of all decision results, mapped by flag keys.</returns>
        public virtual Dictionary<string, OptimizelyDecision> DecideForKeys(string[] keys)
        {
            return DecideForKeys(keys, new OptimizelyDecideOption[] { });
        }

        /// <summary>
        /// Returns a key-map of decision results ({@link OptimizelyDecision}) for all active flag keys.
        /// </summary>
        /// <returns>A dictionary of all decision results, mapped by flag keys.</returns>
        public virtual Dictionary<string, OptimizelyDecision> DecideAll()
        {
            return DecideAll(new OptimizelyDecideOption[] { });
        }

        /// <summary>
        /// Returns a key-map of decision results ({@link OptimizelyDecision}) for all active flag keys.
        /// </summary>
        /// <param name="options">A list of options for decision-making.</param>
        /// <returns>All decision results mapped by flag keys.</returns>
        public virtual Dictionary<string, OptimizelyDecision> DecideAll(OptimizelyDecideOption[] options)
        {
            var optimizelyUserContext = Copy();
            return Optimizely.DecideAll(optimizelyUserContext, options);
        }

        /// <summary>
        /// Track an event.
        /// </summary>
        /// <param name="eventName">The event name.</param>
        public virtual void TrackEvent(string eventName)
        {
            TrackEvent(eventName, new EventTags());
        }

        /// <summary>
        /// Track an event.
        /// </summary>
        /// <param name="eventName">The event name.</param>
        /// <param name="eventTags">A map of event tag names to event tag values.</param>
        public virtual void TrackEvent(string eventName, 
            EventTags eventTags)
        {
            Optimizely.Track(eventName, UserId, Attributes, eventTags);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="flagKey"></param>
        /// <param name="variationKey"></param>
        /// <returns></returns>

        public bool SetForcedDecision(string flagKey, string variationKey)
        {
            return SetForcedDecision(flagKey, null, variationKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="flagKey"></param>
        /// <param name="ruleKey"></param>
        /// <param name="variationKey"></param>
        /// <returns></returns>
        public bool SetForcedDecision(string flagKey, string ruleKey, string variationKey)
        {
            if (Optimizely.GetOptimizelyConfig() == null)
            {
                Logger.Log(LogLevel.ERROR, "Optimizely config is null");
                return false;
            }

            if(ruleKey == null)
            {
                if (ForcedDecisionsMapWithNoRuleKey.TryGetValue(flagKey, out var value))
                {
                    value.VariationKey = variationKey;
                }
                else
                {
                    ForcedDecisionsMapWithNoRuleKey.Add(flagKey, new ForcedDecision(flagKey, null, variationKey));
                }
            }
            else
            {
                Dictionary<string, ForcedDecision> forcedDecision = new Dictionary<string, ForcedDecision>();
                forcedDecision.Add(ruleKey, new ForcedDecision(flagKey, ruleKey, variationKey));
                ForcedDecisionsMap.Add(flagKey, forcedDecision);
            }
            return true;
        }

        /// <summary>
        /// Finds a forced variation
        /// </summary>
        /// <param name="flagKey">The flag key</param>
        /// <returns>The variation key for a forced decision</returns>
        public string GetForcedDecision(string flagKey)
        {
            if (Optimizely.GetOptimizelyConfig() == null)
            {
                Logger.Log(LogLevel.ERROR, "Optimizely SDK not ready.");
                return null;
            }

            return FindForcedDecision(flagKey, null);
        }

        /// <summary>
        /// Finds a forced variation
        /// </summary>
        /// <param name="flagKey">The flag key</param>
        /// <param name="ruleKey">The rule key</param>
        /// <returns>The variation key for a forced decision</returns>
        public string GetForcedDecision(string flagKey, string ruleKey)
        {
            if (Optimizely.GetOptimizelyConfig() == null)
            {
                Logger.Log(LogLevel.ERROR, "Optimizely SDK not ready.");
                return null;
            }

            return FindForcedDecision(flagKey, ruleKey);
        }

        /// <summary>
        /// Finds a forced variation
        /// </summary>
        /// <param name="flagKey">The flag key</param>
        /// <param name="ruleKey">The rule key</param>
        /// <returns>The variation key for a forced decision</returns>
        public string FindForcedDecision(string flagKey, string ruleKey)
        {
            string variationKey = null;
            if (ruleKey != null)
            {
                if (ForcedDecisionsMap.Count > 0 && ForcedDecisionsMap.ContainsKey(flagKey))
                {
                    if (ForcedDecisionsMap.TryGetValue(flagKey, out var forcedDecision) && forcedDecision.TryGetValue(ruleKey, out var forcedDecision2))
                    {
                        variationKey = forcedDecision2.VariationKey;
                    }
                }
            }
            else
            {
                if (ForcedDecisionsMapWithNoRuleKey.Count > 0 && ForcedDecisionsMapWithNoRuleKey.TryGetValue(flagKey, out var forcedDecision))
                {
                    variationKey = forcedDecision.VariationKey;
                }
            }
            return variationKey;
        }
    }
}
