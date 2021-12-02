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
using System;

namespace OptimizelySDK
{
    /// <summary>
    /// OptimizelyUserContext defines user contexts that the SDK will use to make decisions for
    /// </summary>
    public class OptimizelyUserContext
    {
        private ILogger Logger;
        private IErrorHandler ErrorHandler;
        private object mutex = new object();

        // userID for Optimizely user context
        private string UserId;

        // user attributes for Optimizely user context.
        private UserAttributes Attributes;

        // Optimizely object to be used.
        private Optimizely Optimizely;

        private ForcedDecisionsStore ForcedDecisionsStore { get; set; }

        public OptimizelyUserContext(Optimizely optimizely, string userId, UserAttributes userAttributes, IErrorHandler errorHandler, ILogger logger) :
            this(optimizely, userId, userAttributes, null, errorHandler, logger)
        {
        }

        public OptimizelyUserContext(Optimizely optimizely, string userId, UserAttributes userAttributes, ForcedDecisionsStore forcedDecisionsStore, IErrorHandler errorHandler, ILogger logger)
        {
            ErrorHandler = errorHandler;
            Logger = logger;
            Optimizely = optimizely;
            Attributes = userAttributes ?? new UserAttributes();
            ForcedDecisionsStore = forcedDecisionsStore ?? new ForcedDecisionsStore();
            UserId = userId;
        }

        private OptimizelyUserContext Copy() => new OptimizelyUserContext(Optimizely, UserId, GetAttributes(), GetForcedDecisionsStore(), ErrorHandler, Logger);

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
            lock (mutex)
            {
                copiedAttributes = new UserAttributes(Attributes);
            }

            return copiedAttributes;
        }

        /// <summary>
        /// Returns copy of ForcedDecisionsStore associated with UserContext.
        /// </summary>
        /// <returns>copy of ForcedDecisionsStore.</returns>
        public ForcedDecisionsStore GetForcedDecisionsStore()
        {
            ForcedDecisionsStore copiedForcedDecisionsStore = null;
            lock (mutex)
            {
                if (ForcedDecisionsStore.Count == 0)
                {
                    copiedForcedDecisionsStore = ForcedDecisionsStore.NullForcedDecision();
                } else
                {
                    copiedForcedDecisionsStore = new ForcedDecisionsStore(ForcedDecisionsStore);
                }
            }

            return copiedForcedDecisionsStore;
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
        /// Set a forced decision.
        /// </summary>
        /// <param name="context">The context object containing flag and rule key.</param>
        /// <param name="decision">OptimizelyForcedDecision object containing variation key.</param>
        /// <returns></returns>
        public bool SetForcedDecision(OptimizelyDecisionContext context, OptimizelyForcedDecision decision)
        {
            if (!Optimizely.IsValid)
            {
                Logger.Log(LogLevel.ERROR, DecisionMessage.SDK_NOT_READY);
                return false;
            }

            lock (mutex)
            {
                ForcedDecisionsStore[context] = decision;
            }

            return true;
        }

        /// <summary>
        /// Gets a forced variation
        /// </summary>
        /// <param name="context">The context object containing flag and rule key.</param>
        /// <returns>The variation key for a forced decision</returns>
        public OptimizelyForcedDecision GetForcedDecision(OptimizelyDecisionContext context)
        {
            if (!Optimizely.IsValid)
            {
                Logger.Log(LogLevel.ERROR, DecisionMessage.SDK_NOT_READY);
                return null;
            }

            if (context == null || context.FlagKey == null)
            {
                Logger.Log(LogLevel.WARN, "flagKey cannot be null");
                return null;
            }

            if (ForcedDecisionsStore.Count == 0)
            {
                return null;
            }

            OptimizelyForcedDecision decision = null;

            lock (mutex)
            {
                decision = ForcedDecisionsStore[context];
            }
            return decision;
        }

        /// <summary>
        /// Removes a forced decision.
        /// </summary>
        /// <param name="context">The context object containing flag and rule key.</param>
        /// <returns>Whether the item was removed.</returns>
        public bool RemoveForcedDecision(OptimizelyDecisionContext context)
        {
            if (context == null || context.FlagKey == null)
            {
                Logger.Log(LogLevel.WARN, "FlagKey cannot be null");
                return false;
            }

            if (!Optimizely.IsValid)
            {
                Logger.Log(LogLevel.ERROR, DecisionMessage.SDK_NOT_READY);
                return false;
            }
            
            lock (mutex)
            {
                return ForcedDecisionsStore.Remove(context);
            }
        }

        /// <summary>
        /// Removes all forced decisions.
        /// </summary>
        /// <returns>Whether the clear was successful.</returns>
        public bool RemoveAllForcedDecisions()
        {
            if (!Optimizely.IsValid)
            {
                Logger.Log(LogLevel.ERROR, DecisionMessage.SDK_NOT_READY);
                return false;
            }

            lock (mutex)
            {
                ForcedDecisionsStore.RemoveAll();
            }
            return true;
        }

        /// <summary>
        /// Finds a validated forced decision.
        /// </summary>
        /// <param name="context">Object containing flag and rule key of which forced decision is set.</param>
        /// <param name="config">The Project config.</param>
        /// <returns>A result with the variation</returns>
        public Result<Variation> FindValidatedForcedDecision(OptimizelyDecisionContext context, ProjectConfig config)
        {
            DecisionReasons reasons = new DecisionReasons();
            var forcedDecision = GetForcedDecision(context);

            if (config != null && forcedDecision != null)
            {
                var loggingKey = context.RuleKey != null ? "flag (" + context.FlagKey + "), rule (" + context.RuleKey + ")" : "flag (" + context.FlagKey + ")";
                var variationKey = forcedDecision.VariationKey;
                var variation = config.GetFlagVariationByKey(context.FlagKey, variationKey);
                if (variation != null)
                {
                    reasons.AddInfo("Decided by forced decision.");
                    reasons.AddInfo("Variation ({0}) is mapped to {1} and user ({2}) in the forced decision map.", variationKey, loggingKey, UserId);
                    return Result<Variation>.NewResult(variation, reasons);
                }
                else
                {
                    reasons.AddInfo("Invalid variation is mapped to {0} and user ({1}) in the forced decision map.", loggingKey, UserId);
                }
            }
            return Result<Variation>.NullResult(reasons);
        }
    }
}
