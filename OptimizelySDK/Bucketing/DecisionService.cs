/*
* Copyright 2017-2021, Optimizely
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

using System;
using System.Collections.Generic;
using System.Linq;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Bucketing
{
    /// <summary>
    /// Optimizely's decision service that determines which variation of an experiment the user will be allocated to.
    /// The decision service contains all logic around how a user decision is made.
    /// This includes the following:
    /// 1. Checking experiment status
    /// 2. Checking whitelisting
    /// 3. Checking sticky bucketing
    /// 4. Checking audience targeting
    /// 5. Using Murmurhash3 to bucket the user.
    /// </summary>
    public class DecisionService
    {
        public const string LOGGING_KEY_TYPE_EXPERIMENT = "experiment";
        public const string LOGGING_KEY_TYPE_RULE = "rule";

        private Bucketer Bucketer;
        private IErrorHandler ErrorHandler;
        private UserProfileService UserProfileService;
        private ILogger Logger;

        /// <summary>
        /// Associative array of user IDs to an associative array
        /// of experiments to variations.This contains all the forced variations
        /// set by the user by calling setForcedVariation (it is not the same as the
        /// whitelisting forcedVariations data structure in the Experiments class).
        /// </summary>
#if NET35
        private Dictionary<string, Dictionary<string, string>> ForcedVariationMap;
#else
        private System.Collections.Concurrent.ConcurrentDictionary<string, Dictionary<string, string>> ForcedVariationMap;
#endif

        /// <summary>
        ///  Initialize a decision service for the Optimizely client.
        /// </summary>
        /// <param name = "bucketer" > Base bucketer to allocate new users to an experiment.</param>
        /// <param name = "errorHandler" > The error handler of the Optimizely client.</param>
        /// <param name = "userProfileService" ></ param >
        /// < param name= "logger" > UserProfileService implementation for storing user info.</param>
        public DecisionService(Bucketer bucketer, IErrorHandler errorHandler, UserProfileService userProfileService, ILogger logger)
        {
            Bucketer = bucketer;
            ErrorHandler = errorHandler;
            UserProfileService = userProfileService;
            Logger = logger;
#if NET35
            ForcedVariationMap = new Dictionary<string, Dictionary<string, string>>();
#else
            ForcedVariationMap = new System.Collections.Concurrent.ConcurrentDictionary<string, Dictionary<string, string>>();
#endif
        }

        /// <summary>
        /// Get a Variation of an Experiment for a user to be allocated into.
        /// </summary>
        /// <param name = "experiment" > The Experiment the user will be bucketed into.</param>
        /// <param name = "userId" > The userId of the user.
        /// <param name = "filteredAttributes" > The user's attributes. This should be filtered to just attributes in the Datafile.</param>
        /// <returns>The Variation the user is allocated into.</returns>
        public virtual Result<Variation> GetVariation(Experiment experiment,
            OptimizelyUserContext user,
            ProjectConfig config,
            UserAttributes filteredAttributes)
        {
            return GetVariation(experiment, user, config, filteredAttributes, new OptimizelyDecideOption[] { });
        }

        /// <summary>
        /// Get a Variation of an Experiment for a user to be allocated into.
        /// </summary>
        /// <param name = "experiment" > The Experiment the user will be bucketed into.</param>
        /// <param name = "userId" > The userId of the user.
        /// <param name = "filteredAttributes" > The user's attributes. This should be filtered to just attributes in the Datafile.</param>
        /// <returns>The Variation the user is allocated into.</returns>
        public virtual Result<Variation> GetVariation(Experiment experiment,
            OptimizelyUserContext user,
            ProjectConfig config,
            UserAttributes filteredAttributes,
            OptimizelyDecideOption[] options)
        {
            var reasons = new DecisionReasons();
            var userId = user.GetUserId();
            if (!ExperimentUtils.IsExperimentActive(experiment, Logger))
                return Result<Variation>.NullResult(reasons);

            // check if a forced variation is set
            var decisionVariation = GetForcedVariation(experiment.Key, userId, config);
            reasons += decisionVariation.DecisionReasons;
            var variation = decisionVariation?.ResultObject;

            if (variation == null)
            {
                decisionVariation = GetWhitelistedVariation(experiment, user.GetUserId());
                reasons += decisionVariation?.DecisionReasons;

                variation = decisionVariation?.ResultObject;
            }

            if (variation != null)
            {
                decisionVariation.SetReasons(reasons);
                return decisionVariation;
            }

            // fetch the user profile map from the user profile service
            var ignoreUPS = Array.Exists(options, option => option == OptimizelyDecideOption.IGNORE_USER_PROFILE_SERVICE);

            UserProfile userProfile = null;
            if (!ignoreUPS && UserProfileService != null)
            {
                try
                {
                    Dictionary<string, object> userProfileMap = UserProfileService.Lookup(user.GetUserId());
                    if (userProfileMap != null && UserProfileUtil.IsValidUserProfileMap(userProfileMap))
                    {
                        userProfile = UserProfileUtil.ConvertMapToUserProfile(userProfileMap);
                        decisionVariation = GetStoredVariation(experiment, userProfile, config);
                        reasons += decisionVariation?.DecisionReasons;
                        if (decisionVariation?.ResultObject != null) return decisionVariation?.SetReasons(reasons);
                    }
                    else if (userProfileMap == null)
                    {
                        Logger.Log(LogLevel.INFO, reasons.AddInfo("We were unable to get a user profile map from the UserProfileService."));
                    }
                    else
                    {
                        Logger.Log(LogLevel.ERROR, reasons.AddInfo("The UserProfileService returned an invalid map."));
                    }
                }
                catch (Exception exception)
                {
                    Logger.Log(LogLevel.ERROR, reasons.AddInfo(exception.Message));
                    ErrorHandler.HandleError(new Exceptions.OptimizelyRuntimeException(exception.Message));
                }
            }
            var doesUserMeetAudienceConditionsResult = ExperimentUtils.DoesUserMeetAudienceConditions(config, experiment, filteredAttributes, LOGGING_KEY_TYPE_EXPERIMENT, experiment.Key, Logger);
            reasons += doesUserMeetAudienceConditionsResult.DecisionReasons;
            if (doesUserMeetAudienceConditionsResult.ResultObject)
            {
                // Get Bucketing ID from user attributes.
                var bucketingIdResult = GetBucketingId(userId, filteredAttributes);
                reasons += bucketingIdResult.DecisionReasons;

                decisionVariation = Bucketer.Bucket(config, experiment, bucketingIdResult.ResultObject, userId);
                reasons += decisionVariation?.DecisionReasons;

                if (decisionVariation?.ResultObject?.Key != null)
                {
                    if (UserProfileService != null && !ignoreUPS)
                    {
                        var bucketerUserProfile = userProfile ?? new UserProfile(userId, new Dictionary<string, Decision>());
                        SaveVariation(experiment, decisionVariation?.ResultObject, bucketerUserProfile);
                    }
                    else
                        Logger.Log(LogLevel.INFO, "This decision will not be saved since the UserProfileService is null.");
                }

                return decisionVariation?.SetReasons(reasons);
            }
            Logger.Log(LogLevel.INFO, reasons.AddInfo($"User \"{user.GetUserId()}\" does not meet conditions to be in experiment \"{experiment.Key}\"."));

            return Result<Variation>.NullResult(reasons);
        }

        /// <summary>
        /// Gets the forced variation for the given user and experiment.
        /// </summary>
        /// <param name="experimentKey">The experiment key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="config">Project Config</param>
        /// <returns>Variation entity which the given user and experiment should be forced into.</returns>
        public Result<Variation> GetForcedVariation(string experimentKey, string userId, ProjectConfig config)
        {
            var reasons = new DecisionReasons();

            if (ForcedVariationMap.ContainsKey(userId) == false)
            {
                Logger.Log(LogLevel.DEBUG, $@"User ""{userId}"" is not in the forced variation map.");
                return Result<Variation>.NullResult(reasons);
            }

            Dictionary<string, string> experimentToVariationMap = ForcedVariationMap[userId];

            string experimentId = config.GetExperimentFromKey(experimentKey).Id;

            // this case is logged in getExperimentFromKey
            if (string.IsNullOrEmpty(experimentId))
                return Result<Variation>.NullResult(reasons);

            if (experimentToVariationMap.ContainsKey(experimentId) == false)
            {
                Logger.Log(LogLevel.DEBUG, $@"No experiment ""{experimentKey}"" mapped to user ""{userId}"" in the forced variation map.");
                return Result<Variation>.NullResult(reasons);
            }

            string variationId = experimentToVariationMap[experimentId];

            if (string.IsNullOrEmpty(variationId))
            {
                Logger.Log(LogLevel.DEBUG, $@"No variation mapped to experiment ""{experimentKey}"" in the forced variation map.");
                return Result<Variation>.NullResult(reasons);
            }

            string variationKey = config.GetVariationFromId(experimentKey, variationId).Key;

            // this case is logged in getVariationFromKey
            if (string.IsNullOrEmpty(variationKey))
                return Result<Variation>.NullResult(reasons);
            Logger.Log(LogLevel.DEBUG, reasons.AddInfo($@"Variation ""{variationKey}"" is mapped to experiment ""{experimentKey}"" and user ""{userId}"" in the forced variation map"));

            Variation variation = config.GetVariationFromKey(experimentKey, variationKey);

            return Result<Variation>.NewResult(variation, reasons);
        }

        /// <summary>
        /// Sets an associative array of user IDs to an associative array of experiments to forced variations.
        /// </summary>
        /// <param name="experimentKey">The experiment key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="variationKey">The variation key</param>
        /// <param name="config">Project Config</param>
        /// <returns>A boolean value that indicates if the set completed successfully.</returns>
        public bool SetForcedVariation(string experimentKey, string userId, string variationKey, ProjectConfig config)
        {
            // Empty variation key is considered as invalid.
            if (variationKey != null && variationKey.Length == 0)
            {
                Logger.Log(LogLevel.DEBUG, "Variation key is invalid.");
                return false;
            }

            var experimentId = config.GetExperimentFromKey(experimentKey).Id;

            // this case is logged in getExperimentFromKey
            if (string.IsNullOrEmpty(experimentId))
                return false;

            // clear the forced variation if the variation key is null
            if (variationKey == null)
            {
                if (ForcedVariationMap.ContainsKey(userId) && ForcedVariationMap[userId].ContainsKey(experimentId))
                    ForcedVariationMap[userId].Remove(experimentId);

                Logger.Log(LogLevel.DEBUG, $@"Variation mapped to experiment ""{experimentKey}"" has been removed for user ""{userId}"".");
                return true;
            }

            string variationId = config.GetVariationFromKey(experimentKey, variationKey).Id;

            // this case is logged in getVariationFromKey
            if (string.IsNullOrEmpty(variationId))
                return false;

            // Add User if not exist.
            if (ForcedVariationMap.ContainsKey(userId) == false)
                ForcedVariationMap[userId] = new Dictionary<string, string>();

            // Add/Replace Experiment to Variation ID map.
            ForcedVariationMap[userId][experimentId] = variationId;

            Logger.Log(LogLevel.DEBUG, $@"Set variation ""{variationId}"" for experiment ""{experimentId}"" and user ""{userId}"" in the forced variation map.");
            return true;
        }

        /// <summary>
        /// Get the variation the user has been whitelisted into.
        /// </summary>
        /// <param name = "experiment" >in which user is to be bucketed.</param>
        /// <param name = "userId" > User Identifier</param>
        /// <param name = "reasons" > Decision log messages.</param>
        /// <returns>if the user is not whitelisted into any variation {@link Variation}
        /// the user is bucketed into if the user has a specified whitelisted variation.</returns>
        public Result<Variation> GetWhitelistedVariation(Experiment experiment, string userId)
        {
            var reasons = new DecisionReasons();

            //if a user has a forced variation mapping, return the respective variation
            Dictionary<string, string> userIdToVariationKeyMap = experiment.UserIdToKeyVariations;

            if (!userIdToVariationKeyMap.ContainsKey(userId))
                return Result<Variation>.NullResult(reasons);

            string forcedVariationKey = userIdToVariationKeyMap[userId];
            Variation forcedVariation = experiment.VariationKeyToVariationMap.ContainsKey(forcedVariationKey)
                ? experiment.VariationKeyToVariationMap[forcedVariationKey]
                : null;

            if (forcedVariation != null)
                Logger.Log(LogLevel.INFO, reasons.AddInfo($"User \"{userId}\" is forced in variation \"{forcedVariationKey}\"."));
            else
                Logger.Log(LogLevel.ERROR, reasons.AddInfo($"Variation \"{forcedVariationKey}\" is not in the datafile. Not activating user \"{userId}\"."));

            return Result<Variation>.NewResult(forcedVariation, reasons);
        }

        /// <summary>
        /// Get the { @link Variation } that has been stored for the user in the { @link UserProfileService } implementation.
        /// </summary>
        /// <param name = "experiment" > which the user was bucketed</param>
        /// <param name = "userProfile" > User profile of the user</param>
        /// <returns>The user was previously bucketed into.</returns>
        public Result<Variation> GetStoredVariation(Experiment experiment, UserProfile userProfile, ProjectConfig config)
        {
            // ---------- Check User Profile for Sticky Bucketing ----------
            // If a user profile instance is present then check it for a saved variation
            string experimentId = experiment.Id;
            string experimentKey = experiment.Key;

            var reasons = new DecisionReasons();

            Decision decision = userProfile.ExperimentBucketMap.ContainsKey(experimentId) ?
            userProfile.ExperimentBucketMap[experimentId] : null;

            if (decision == null)
            {
                Logger.Log(LogLevel.INFO, reasons.AddInfo($"No previously activated variation of experiment \"{experimentKey}\" for user \"{userProfile.UserId}\" found in user profile."));
                return Result<Variation>.NullResult(reasons);
            }

            try
            {
                string variationId = decision.VariationId;

                Variation savedVariation = config.ExperimentIdMap[experimentId].VariationIdToVariationMap.ContainsKey(variationId)
                    ? config.ExperimentIdMap[experimentId].VariationIdToVariationMap[variationId]
                    : null;

                if (savedVariation == null)
                {
                    Logger.Log(LogLevel.INFO, reasons.AddInfo($"User \"{userProfile.UserId}\" was previously bucketed into variation with ID \"{variationId}\" for experiment \"{experimentId}\", but no matching variation was found for that user. We will re-bucket the user."));
                    return Result<Variation>.NullResult(reasons);
                }

                Logger.Log(LogLevel.INFO, reasons.AddInfo($"Returning previously activated variation \"{savedVariation.Key}\" of experiment \"{experimentKey}\" for user \"{userProfile.UserId}\" from user profile."));
                return Result<Variation>.NewResult(savedVariation, reasons);
            }
            catch (Exception)
            {
                return Result<Variation>.NullResult(reasons);
            }
        }

        /// <summary>
        /// Save a { @link Variation } of an { @link Experiment } for a user in the {@link UserProfileService}.
        /// </summary>
        /// <param name = "experiment" > The experiment the user was buck</param>
        /// <param name = "variation" > The Variation to save.</param>
        /// <param name = "userProfile" > instance of the user information.</param>
        public void SaveVariation(Experiment experiment, Variation variation, UserProfile userProfile)
        {
            //only save if the user has implemented a user profile service
            if (UserProfileService == null)
                return;

            Decision decision;
            if (userProfile.ExperimentBucketMap.ContainsKey(experiment.Id))
            {
                decision = userProfile.ExperimentBucketMap[experiment.Id];
                decision.VariationId = variation.Id;
            }
            else
            {
                decision = new Decision(variation.Id);
            }

            userProfile.ExperimentBucketMap[experiment.Id] = decision;

            try
            {
                UserProfileService.Save(userProfile.ToMap());
                Logger.Log(LogLevel.INFO, $"Saved variation \"{variation.Id}\" of experiment \"{experiment.Id}\" for user \"{userProfile.UserId}\".");
            }
            catch (Exception exception)
            {
                Logger.Log(LogLevel.ERROR, $"Failed to save variation \"{variation.Id}\" of experiment \"{experiment.Id}\" for user \"{userProfile.UserId}\".");
                ErrorHandler.HandleError(new Exceptions.OptimizelyRuntimeException(exception.Message));
            }
        }

        /// <summary>
        /// Try to bucket the user into a rollout rule.
        /// Evaluate the user for rules in priority order by seeing if the user satisfies the audience.
        /// Fall back onto the everyone else rule if the user is ever excluded from a rule due to traffic allocation.
        /// </summary>
        /// <param name = "featureFlag" >The feature flag the user wants to access.</param>
        /// <param name = "userId" >User Identifier</param>
        /// <param name = "filteredAttributes" >The user's attributes. This should be filtered to just attributes in the Datafile.</param>
        /// <param name = "reasons" >Decision log messages.</param>
        /// <returns>null if the user is not bucketed into the rollout or if the feature flag was not attached to a rollout.
        /// otherwise the FeatureDecision entity</returns>
        public virtual Result<FeatureDecision> GetVariationForFeatureRollout(FeatureFlag featureFlag,
            OptimizelyUserContext user,
            ProjectConfig config)
        {
            var reasons = new DecisionReasons();

            if (featureFlag == null)
            {
                Logger.Log(LogLevel.ERROR, "Invalid feature flag provided.");
                return Result<FeatureDecision>.NullResult(reasons);
            }

            if (string.IsNullOrEmpty(featureFlag.RolloutId))
            {
                Logger.Log(LogLevel.INFO, reasons.AddInfo($"The feature flag \"{featureFlag.Key}\" is not used in a rollout."));
                return Result<FeatureDecision>.NullResult(reasons);
            }

            Rollout rollout = config.GetRolloutFromId(featureFlag.RolloutId);

            if (string.IsNullOrEmpty(rollout.Id))
            {
                Logger.Log(LogLevel.ERROR, reasons.AddInfo($"The rollout with id \"{featureFlag.RolloutId}\" is not found in the datafile for feature flag \"{featureFlag.Key}\""));
                return Result<FeatureDecision>.NullResult(reasons);
            }

            var rolloutRulesLength = rollout.Experiments.Count;
            var rolloutRules = rollout.Experiments;

            if (rolloutRules == null || rolloutRulesLength == 0)
            {
                return Result<FeatureDecision>.NullResult(reasons);
            }

            var index = 0;
            while (index < rolloutRulesLength)
            {
                var decisionResult = GetVariationFromDeliveryRule(config, featureFlag.Key, rolloutRules, index, user);
                reasons += decisionResult.DecisionReasons;

                if (decisionResult.ResultObject?.Variation?.Key != null)
                {
                    return Result<FeatureDecision>.NewResult(new FeatureDecision(rolloutRules[index], decisionResult.ResultObject.Variation, FeatureDecision.DECISION_SOURCE_ROLLOUT), reasons);
                }

                // the last rule is special for "Everyone Else"
                index = decisionResult.SkipToEveryoneElse ? (rolloutRulesLength - 1) : (index + 1);
            }
            return Result<FeatureDecision>.NullResult(reasons);
        }


        /// <summary>
        /// Get the variation if the user is bucketed for one of the experiments on this feature flag.
        /// </summary>
        /// <param name = "featureFlag" >The feature flag the user wants to access.</param>
        /// <param name = "userId" >User Identifier</param>
        /// <param name = "filteredAttributes" >The user's attributes. This should be filtered to just attributes in the Datafile.</param>
        /// <returns>null if the user is not bucketed into the rollout or if the feature flag was not attached to a rollout.
        /// Otherwise the FeatureDecision entity</returns>
        public virtual Result<FeatureDecision> GetVariationForFeatureExperiment(FeatureFlag featureFlag,
            OptimizelyUserContext user,
            UserAttributes filteredAttributes,
            ProjectConfig config,
            OptimizelyDecideOption[] options)
        {
            var reasons = new DecisionReasons();
            var userId = user.GetUserId();
            if (featureFlag == null)
            {
                Logger.Log(LogLevel.ERROR, "Invalid feature flag provided.");
                return Result<FeatureDecision>.NullResult(reasons);
            }

            if (featureFlag.ExperimentIds == null || featureFlag.ExperimentIds.Count == 0)
            {
                Logger.Log(LogLevel.INFO, reasons.AddInfo($"The feature flag \"{featureFlag.Key}\" is not used in any experiments."));
                return Result<FeatureDecision>.NullResult(reasons);
            }

            foreach (var experimentId in featureFlag.ExperimentIds)
            {
                var experiment = config.GetExperimentFromId(experimentId);

                if (string.IsNullOrEmpty(experiment.Key))
                    continue;

                var variationResult = GetVariationFromExperiment(config, featureFlag, experiment, user, options);
                reasons += variationResult.DecisionReasons;

                if (variationResult?.ResultObject?.Experiment != null && variationResult?.ResultObject?.Variation?.Id != null)
                {
                    Logger.Log(LogLevel.INFO, reasons.AddInfo($"The user \"{userId}\" is bucketed into experiment \"{experiment.Key}\" of feature \"{featureFlag.Key}\"."));
                    return Result<FeatureDecision>.NewResult(new FeatureDecision(experiment, variationResult.ResultObject.Variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST), reasons);
                }
            }

            Logger.Log(LogLevel.INFO, reasons.AddInfo($"The user \"{userId}\" is not bucketed into any of the experiments on the feature \"{featureFlag.Key}\"."));
            return Result<FeatureDecision>.NullResult(reasons);
        }

        

        private Result<FeatureDecision> GetVariationFromExperiment(ProjectConfig config, FeatureFlag flag, Experiment experiment, OptimizelyUserContext user, OptimizelyDecideOption[] options)
        {
            var reasons = new DecisionReasons();

            if (flag.ExperimentIds.Any())
            {
                foreach (var expId in flag.ExperimentIds)
                {
                    config.ExperimentIdMap.TryGetValue(expId, out var exp);

                    var decisionVariation = GetVariationFromExperimentRule(config, flag.Key, experiment, user, options);
                    reasons += decisionVariation?.DecisionReasons;

                    var variation = decisionVariation?.ResultObject;

                    if (variation != null)
                    {
                        var featureDecision = new FeatureDecision(exp, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);

                        return Result<FeatureDecision>.NewResult(featureDecision, reasons);
                    }
                }
            }

            return Result<FeatureDecision>.NullResult(reasons);
        }

        private Result<FeatureDecision> GetVariationFromDeliveryRule(ProjectConfig config, string key, List<Experiment> rules, int ruleIndex, OptimizelyUserContext user)
        {
            var reasons = new DecisionReasons();

            bool skipToEveryoneElse = false;

            //Check forced decision first
            var rule = rules[ruleIndex];
            var decisionContext = new OptimizelyDecisionContext(key, rule.Key);
            var forcedDecisionResponse = user.FindValidatedForcedDecision(decisionContext);

            reasons += forcedDecisionResponse.DecisionReasons;
            if (forcedDecisionResponse.ResultObject != null)
            {
                return Result<FeatureDecision>.NewResult(new FeatureDecision(rule, forcedDecisionResponse.ResultObject, null), skipToEveryoneElse, reasons);
            }

            // Regular decision
            var userId = user.GetUserId();
            var attributes = user.GetAttributes();

            // Get Bucketing ID from user attributes.
            var bucketingIdResult = GetBucketingId(userId, attributes);
            reasons += bucketingIdResult.DecisionReasons;

            var everyoneElse = ruleIndex == rules.Count - 1;

            var loggingKey = everyoneElse ? "Everyone Else" : ruleIndex + 1 + "";

            Result<Variation> bucketedVariation = null;

            // Evaluate if user meets the audience condition of this rollout rule
            var doesUserMeetAudienceConditionsResult = ExperimentUtils.DoesUserMeetAudienceConditions(config, rule, attributes, LOGGING_KEY_TYPE_RULE, rule.Key, Logger);
            reasons += doesUserMeetAudienceConditionsResult.DecisionReasons;
            if (doesUserMeetAudienceConditionsResult.ResultObject)
            {
                Logger.Log(LogLevel.INFO, reasons.AddInfo($"User \"{userId}\" meets condition for targeting rule \"{loggingKey}\"."));

                bucketedVariation = Bucketer.Bucket(config, rule, bucketingIdResult.ResultObject, userId);
                reasons += bucketedVariation?.DecisionReasons;

                if (bucketedVariation?.ResultObject?.Key != null)
                {
                    Logger.Log(LogLevel.INFO, reasons.AddInfo($"User \"{userId}\" is in the traffic group of targeting rule \"{loggingKey}\"."));
                }
                else if (!everyoneElse)
                {
                    //skip this loggng for everyoneElse rule since this has a message not for everyoneElse
                    Logger.Log(LogLevel.INFO, reasons.AddInfo($"User \"{userId}\" is not in the traffic group for targeting rule \"{loggingKey}\". Checking EveryoneElse rule now."));
                    skipToEveryoneElse = true;
                }
            }
            else
            {
                Logger.Log(LogLevel.DEBUG, reasons.AddInfo($"User \"{userId}\" does not meet the conditions for targeting rule \"{loggingKey}\"."));
            }

            return Result<FeatureDecision>.NewResult(new FeatureDecision(rule, bucketedVariation?.ResultObject, null), skipToEveryoneElse, reasons);
        }
        private Result<Variation> GetVariationFromExperimentRule(ProjectConfig config, string key, Experiment experiment, OptimizelyUserContext user, OptimizelyDecideOption[] options)
        {
            var reasons = new DecisionReasons();

            var ruleKey = experiment != null ? experiment.Key : null;

            var decisionContext = new OptimizelyDecisionContext(key, ruleKey);

            var forcedDecisionResponse = user.FindValidatedForcedDecision(decisionContext);

            reasons += forcedDecisionResponse.DecisionReasons;

            var variation = forcedDecisionResponse?.ResultObject;

            if (variation != null)
            {
                return Result<Variation>.NewResult(variation, reasons);
            }

            var decisionResponse = GetVariation(experiment, user, config, user.GetAttributes(), options);

            reasons += decisionResponse?.DecisionReasons;

            return Result<Variation>.NewResult(decisionResponse?.ResultObject, reasons);
        }

        /// <summary>
        /// Get the variation the user is bucketed into for the FeatureFlag
        /// </summary>
        /// <param name = "featureFlag" >The feature flag the user wants to access.</param>
        /// <param name = "userId" >User Identifier</param>
        /// <param name = "filteredAttributes" >The user's attributes. This should be filtered to just attributes in the Datafile.</param>
        /// <returns>null if the user is not bucketed into any variation or the FeatureDecision entity if the user is
        /// successfully bucketed.</returns>
        public virtual Result<FeatureDecision> GetVariationForFeature(FeatureFlag featureFlag, OptimizelyUserContext user, ProjectConfig config)
        {
            return GetVariationForFeature(featureFlag, user, config, user.GetAttributes(), new OptimizelyDecideOption[] { });
        }

        /// <summary>
        /// Get the variation the user is bucketed into for the FeatureFlag
        /// </summary>
        /// <param name = "featureFlag" >The feature flag the user wants to access.</param>
        /// <param name = "userId" >User Identifier</param>
        /// <param name = "filteredAttributes" >The user's attributes. This should be filtered to just attributes in the Datafile.</param>
        /// <param name = "filteredAttributes" >The user's attributes. This should be filtered to just attributes in the Datafile.</param>
        /// <param name = "options" >An array of decision options.</param>
        /// <returns>null if the user is not bucketed into any variation or the FeatureDecision entity if the user is
        /// successfully bucketed.</returns>
        public virtual Result<FeatureDecision> GetVariationForFeature(FeatureFlag featureFlag,
            OptimizelyUserContext user,
            ProjectConfig config,
            UserAttributes filteredAttributes,
            OptimizelyDecideOption[] options)
        {
            var reasons = new DecisionReasons();
            var userId = user.GetUserId();
            // Check if the feature flag has an experiment and the user is bucketed into that experiment.
            var decisionResult = GetVariationForFeatureExperiment(featureFlag, user, filteredAttributes, config, options);
            reasons += decisionResult.DecisionReasons;

            if (decisionResult.ResultObject != null)
            {
                return Result<FeatureDecision>.NewResult(decisionResult.ResultObject, reasons);
            }

            // Check if the feature flag has rollout and the the user is bucketed into one of its rules.
            decisionResult = GetVariationForFeatureRollout(featureFlag, user, config);
            reasons += decisionResult.DecisionReasons;

            if (decisionResult.ResultObject != null)
            {
                Logger.Log(LogLevel.INFO, reasons.AddInfo($"The user \"{userId}\" is bucketed into a rollout for feature flag \"{featureFlag.Key}\"."));
                return Result<FeatureDecision>.NewResult(decisionResult.ResultObject, reasons);
            }

            Logger.Log(LogLevel.INFO, reasons.AddInfo($"The user \"{userId}\" is not bucketed into a rollout for feature flag \"{featureFlag.Key}\"."));
            return Result<FeatureDecision>.NullResult(reasons);
        }

        /// <summary>
        /// Get Bucketing ID from user attributes.
        /// </summary>
        /// <param name = "userId" >User Identifier</param>
        /// <param name = "filteredAttributes" >The user's attributes.</param>
        /// <returns>Bucketing Id if it is a string type in attributes, user Id otherwise.</returns>
        private Result<string> GetBucketingId(string userId, UserAttributes filteredAttributes)
        {
            var reasons = new DecisionReasons();
            string bucketingId = userId;

            // If the bucketing ID key is defined in attributes, then use that in place of the userID for the murmur hash key
            if (filteredAttributes != null && filteredAttributes.ContainsKey(ControlAttributes.BUCKETING_ID_ATTRIBUTE))
            {
                if (filteredAttributes[ControlAttributes.BUCKETING_ID_ATTRIBUTE] is string)
                {
                    bucketingId = (string)filteredAttributes[ControlAttributes.BUCKETING_ID_ATTRIBUTE];
                    Logger.Log(LogLevel.DEBUG, $"BucketingId is valid: \"{bucketingId}\"");
                }
                else
                {
                    Logger.Log(LogLevel.WARN, reasons.AddInfo("BucketingID attribute is not a string. Defaulted to userId"));
                }
            }

            return Result<string>.NewResult(bucketingId, reasons);
        }
    }
}
