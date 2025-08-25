/*
* Copyright 2017-2022, 2024 Optimizely
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

using System;
using System.Collections.Generic;
using System.Linq;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;
using OptimizelySDK.Utils;
using static OptimizelySDK.Entity.Holdout;

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
        private System.Collections.Concurrent.ConcurrentDictionary<string,
            Dictionary<string, string>> ForcedVariationMap;
#endif

        /// <summary>
        ///  Initialize a decision service for the Optimizely client.
        /// </summary>
        /// <param name = "bucketer" > Base bucketer to allocate new users to an experiment.</param>
        /// <param name = "errorHandler" > The error handler of the Optimizely client.</param>
        /// <param name = "userProfileService" ></ param >
        /// < param name= "logger" > UserProfileService implementation for storing user info.</param>
        public DecisionService(Bucketer bucketer, IErrorHandler errorHandler,
            UserProfileService userProfileService, ILogger logger
        )
        {
            Bucketer = bucketer;
            ErrorHandler = errorHandler;
            UserProfileService = userProfileService;
            Logger = logger;
#if NET35
            ForcedVariationMap = new Dictionary<string, Dictionary<string, string>>();
#else
            ForcedVariationMap =
                new System.Collections.Concurrent.ConcurrentDictionary<string,
                    Dictionary<string, string>>();
#endif
        }

        /// <summary>
        /// Get a Variation of an Experiment for a user to be allocated into.
        /// </summary>
        /// <param name="experiment">The Experiment the user will be bucketed into.</param>
        /// <param name="user">Optimizely user context.</param>
        /// <param name="config">Project config.</param>
        /// <returns>The Variation the user is allocated into.</returns>
        public virtual Result<Variation> GetVariation(Experiment experiment,
            OptimizelyUserContext user,
            ProjectConfig config
        )
        {
            return GetVariation(experiment, user, config, new OptimizelyDecideOption[] { });
        }

        /// <summary>
        /// Get a Variation of an Experiment for a user to be allocated into.
        /// </summary>
        /// <param name="experiment">The Experiment the user will be bucketed into.</param>
        /// <param name="user">Optimizely user context.</param>
        /// <param name="config">Project Config.</param>
        /// <param name="options">An array of decision options.</param>
        /// <returns></returns>
        public virtual Result<Variation> GetVariation(Experiment experiment,
            OptimizelyUserContext user,
            ProjectConfig config,
            OptimizelyDecideOption[] options
        )
        {
            var reasons = new DecisionReasons();

            var ignoreUps = options.Contains(OptimizelyDecideOption.IGNORE_USER_PROFILE_SERVICE);
            UserProfileTracker userProfileTracker = null;

            if (UserProfileService != null && !ignoreUps)
            {
                userProfileTracker = new UserProfileTracker(UserProfileService, user.GetUserId(),
                    Logger, ErrorHandler);
                userProfileTracker.LoadUserProfile(reasons);
            }

            var response = GetVariation(experiment, user, config, options, userProfileTracker,
                reasons);

            if (UserProfileService != null && !ignoreUps &&
                userProfileTracker?.ProfileUpdated == true)
            {
                userProfileTracker.SaveUserProfile();
            }

            return response;
        }

        /// <summary>
        /// Get a Variation of an Experiment for a user to be allocated into.
        /// </summary>
        /// <param name="experiment">The Experiment the user will be bucketed into.</param>
        /// <param name="user">Optimizely user context.</param>
        /// <param name="config">Project Config.</param>
        /// <param name="options">An array of decision options.</param>
        /// <param name="userProfileTracker">A UserProfileTracker object.</param>
        /// <param name="reasons">Set of reasons for the decision.</param>
        /// <returns>The Variation the user is allocated into.</returns>
        public virtual Result<Variation> GetVariation(Experiment experiment,
            OptimizelyUserContext user,
            ProjectConfig config,
            OptimizelyDecideOption[] options,
            UserProfileTracker userProfileTracker,
            DecisionReasons reasons = null
        )
        {
            if (reasons == null)
            {
                reasons = new DecisionReasons();
            }

            if (!ExperimentUtils.IsExperimentActive(experiment, Logger))
            {
                var message = reasons.AddInfo($"Experiment {experiment.Key} is not running.");
                Logger.Log(LogLevel.INFO, message);
                return Result<Variation>.NullResult(reasons);
            }

            var userId = user.GetUserId();

            var decisionVariation = GetForcedVariation(experiment.Key, userId, config);
            reasons += decisionVariation.DecisionReasons;
            var variation = decisionVariation.ResultObject;

            if (variation == null)
            {
                decisionVariation = GetWhitelistedVariation(experiment, user.GetUserId());
                reasons += decisionVariation.DecisionReasons;
                variation = decisionVariation.ResultObject;
            }

            if (variation != null)
            {
                decisionVariation.SetReasons(reasons);
                return decisionVariation;
            }

            if (userProfileTracker != null)
            {
                decisionVariation =
                    GetStoredVariation(experiment, userProfileTracker.UserProfile, config);
                reasons += decisionVariation.DecisionReasons;
                variation = decisionVariation.ResultObject;
                if (variation != null)
                {
                    return decisionVariation;
                }
            }

            var decisionMeetAudience = ExperimentUtils.DoesUserMeetAudienceConditions(config,
                experiment, user,
                LOGGING_KEY_TYPE_EXPERIMENT, experiment.Key, Logger);
            reasons += decisionMeetAudience.DecisionReasons;
            if (decisionMeetAudience.ResultObject)
            {
                var bucketingId = GetBucketingId(userId, user.GetAttributes()).ResultObject;

                decisionVariation = Bucketer.Bucket(config, experiment, bucketingId, userId);
                reasons += decisionVariation.DecisionReasons;
                variation = decisionVariation.ResultObject;

                if (variation != null)
                {
                    if (userProfileTracker != null)
                    {
                        userProfileTracker.UpdateUserProfile(experiment, variation);
                    }
                    else
                    {
                        Logger.Log(LogLevel.INFO,
                            "This decision will not be saved since the UserProfileService is null.");
                    }
                }

                return decisionVariation.SetReasons(reasons);
            }

            Logger.Log(LogLevel.INFO,
                reasons.AddInfo(
                    $"User \"{user.GetUserId()}\" does not meet conditions to be in experiment \"{experiment.Key}\"."));

            return Result<Variation>.NullResult(reasons);
        }

        /// <summary>
        /// Gets the forced variation for the given user and experiment.
        /// </summary>
        /// <param name="experimentKey">The experiment key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="config">Project Config</param>
        /// <returns>Variation entity which the given user and experiment should be forced into.</returns>
        public Result<Variation> GetForcedVariation(string experimentKey, string userId,
            ProjectConfig config
        )
        {
            var reasons = new DecisionReasons();

            if (ForcedVariationMap.ContainsKey(userId) == false)
            {
                Logger.Log(LogLevel.DEBUG,
                    $@"User ""{userId}"" is not in the forced variation map.");
                return Result<Variation>.NullResult(reasons);
            }

            var experimentToVariationMap = ForcedVariationMap[userId];

            var experimentId = config.GetExperimentFromKey(experimentKey).Id;

            // this case is logged in getExperimentFromKey
            if (string.IsNullOrEmpty(experimentId))
            {
                return Result<Variation>.NullResult(reasons);
            }

            if (experimentToVariationMap.ContainsKey(experimentId) == false)
            {
                Logger.Log(LogLevel.DEBUG,
                    $@"No experiment ""{experimentKey}"" mapped to user ""{userId}"" in the forced variation map.");
                return Result<Variation>.NullResult(reasons);
            }

            var variationId = experimentToVariationMap[experimentId];

            if (string.IsNullOrEmpty(variationId))
            {
                Logger.Log(LogLevel.DEBUG,
                    $@"No variation mapped to experiment ""{experimentKey}"" in the forced variation map.");
                return Result<Variation>.NullResult(reasons);
            }

            var variationKey = config.GetVariationFromId(experimentKey, variationId).Key;

            // this case is logged in getVariationFromKey
            if (string.IsNullOrEmpty(variationKey))
            {
                return Result<Variation>.NullResult(reasons);
            }

            Logger.Log(LogLevel.DEBUG,
                reasons.AddInfo($@"Variation ""{variationKey}"" is mapped to experiment ""{experimentKey}"" and user ""{userId}"" in the forced variation map"));

            var variation = config.GetVariationFromKey(experimentKey, variationKey);

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
        public bool SetForcedVariation(string experimentKey, string userId, string variationKey,
            ProjectConfig config
        )
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
            {
                return false;
            }

            // clear the forced variation if the variation key is null
            if (variationKey == null)
            {
                if (ForcedVariationMap.ContainsKey(userId) &&
                    ForcedVariationMap[userId].ContainsKey(experimentId))
                {
                    ForcedVariationMap[userId].Remove(experimentId);
                }

                Logger.Log(LogLevel.DEBUG,
                    $@"Variation mapped to experiment ""{experimentKey}"" has been removed for user ""{userId}"".");
                return true;
            }

            var variationId = config.GetVariationFromKey(experimentKey, variationKey).Id;

            // this case is logged in getVariationFromKey
            if (string.IsNullOrEmpty(variationId))
            {
                return false;
            }

            // Add User if not exist.
            if (ForcedVariationMap.ContainsKey(userId) == false)
            {
                ForcedVariationMap[userId] = new Dictionary<string, string>();
            }

            // Add/Replace Experiment to Variation ID map.
            ForcedVariationMap[userId][experimentId] = variationId;

            Logger.Log(LogLevel.DEBUG,
                $@"Set variation ""{variationId}"" for experiment ""{experimentId}"" and user ""{userId}"" in the forced variation map.");
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
            var userIdToVariationKeyMap = experiment.UserIdToKeyVariations;

            if (!userIdToVariationKeyMap.ContainsKey(userId))
            {
                return Result<Variation>.NullResult(reasons);
            }

            var forcedVariationKey = userIdToVariationKeyMap[userId];
            var forcedVariation =
                experiment.VariationKeyToVariationMap.ContainsKey(forcedVariationKey) ?
                    experiment.VariationKeyToVariationMap[forcedVariationKey] :
                    null;

            if (forcedVariation != null)
            {
                Logger.Log(LogLevel.INFO,
                    reasons.AddInfo(
                        $"User \"{userId}\" is forced in variation \"{forcedVariationKey}\"."));
            }
            else
            {
                Logger.Log(LogLevel.ERROR,
                    reasons.AddInfo(
                        $"Variation \"{forcedVariationKey}\" is not in the datafile. Not activating user \"{userId}\"."));
            }

            return Result<Variation>.NewResult(forcedVariation, reasons);
        }

        /// <summary>
        /// Get the { @link Variation } that has been stored for the user in the { @link UserProfileService } implementation.
        /// </summary>
        /// <param name = "experiment" > which the user was bucketed</param>
        /// <param name = "userProfile" > User profile of the user</param>
        /// <returns>The user was previously bucketed into.</returns>
        public Result<Variation> GetStoredVariation(Experiment experiment, UserProfile userProfile,
            ProjectConfig config
        )
        {
            // ---------- Check User Profile for Sticky Bucketing ----------
            // If a user profile instance is present then check it for a saved variation
            var experimentId = experiment.Id;
            var experimentKey = experiment.Key;

            var reasons = new DecisionReasons();

            var decision = userProfile.ExperimentBucketMap.ContainsKey(experimentId) ?
                userProfile.ExperimentBucketMap[experimentId] :
                null;

            if (decision == null)
            {
                Logger.Log(LogLevel.INFO,
                    reasons.AddInfo(
                        $"No previously activated variation of experiment \"{experimentKey}\" for user \"{userProfile.UserId}\" found in user profile."));
                return Result<Variation>.NullResult(reasons);
            }

            try
            {
                var variationId = decision.VariationId;

                var savedVariation =
                    config.ExperimentIdMap[experimentId].
                        VariationIdToVariationMap.ContainsKey(variationId) ?
                        config.ExperimentIdMap[experimentId].
                            VariationIdToVariationMap[variationId] :
                        null;

                if (savedVariation == null)
                {
                    Logger.Log(LogLevel.INFO,
                        reasons.AddInfo(
                            $"User \"{userProfile.UserId}\" was previously bucketed into variation with ID \"{variationId}\" for experiment \"{experimentId}\", but no matching variation was found for that user. We will re-bucket the user."));
                    return Result<Variation>.NullResult(reasons);
                }

                Logger.Log(LogLevel.INFO,
                    reasons.AddInfo(
                        $"Returning previously activated variation \"{savedVariation.Key}\" of experiment \"{experimentKey}\" for user \"{userProfile.UserId}\" from user profile."));
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
        public void SaveVariation(Experiment experiment, Variation variation,
            UserProfile userProfile
        )
        {
            //only save if the user has implemented a user profile service
            if (UserProfileService == null)
            {
                return;
            }

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
                Logger.Log(LogLevel.INFO,
                    $"Saved variation \"{variation.Id}\" of experiment \"{experiment.Id}\" for user \"{userProfile.UserId}\".");
            }
            catch (Exception exception)
            {
                Logger.Log(LogLevel.ERROR,
                    $"Failed to save variation \"{variation.Id}\" of experiment \"{experiment.Id}\" for user \"{userProfile.UserId}\".");
                ErrorHandler.HandleError(
                    new Exceptions.OptimizelyRuntimeException(exception.Message));
            }
        }

        /// <summary>
        /// Try to bucket the user into a rollout rule.
        /// Evaluate the user for rules in priority order by seeing if the user satisfies the audience.
        /// Fall back onto the everyone else rule if the user is ever excluded from a rule due to traffic allocation.
        /// </summary>
        /// <param name = "featureFlag" >The feature flag the user wants to access.</param>
        /// <param name = "user" >The user context.</param>
        /// <returns>null if the user is not bucketed into the rollout or if the feature flag was not attached to a rollout.
        /// otherwise the FeatureDecision entity</returns>
        public virtual Result<FeatureDecision> GetVariationForFeatureRollout(
            FeatureFlag featureFlag,
            OptimizelyUserContext user,
            ProjectConfig config
        )
        {
            var reasons = new DecisionReasons();

            if (featureFlag == null)
            {
                Logger.Log(LogLevel.ERROR, "Invalid feature flag provided.");
                return Result<FeatureDecision>.NullResult(reasons);
            }

            if (string.IsNullOrEmpty(featureFlag.RolloutId))
            {
                Logger.Log(LogLevel.INFO,
                    reasons.AddInfo(
                        $"The feature flag \"{featureFlag.Key}\" is not used in a rollout."));
                return Result<FeatureDecision>.NullResult(reasons);
            }

            var rollout = config.GetRolloutFromId(featureFlag.RolloutId);

            if (string.IsNullOrEmpty(rollout.Id))
            {
                Logger.Log(LogLevel.ERROR,
                    reasons.AddInfo(
                        $"The rollout with id \"{featureFlag.RolloutId}\" is not found in the datafile for feature flag \"{featureFlag.Key}\""));
                return Result<FeatureDecision>.NullResult(reasons);
            }

            if (rollout.Experiments == null || rollout.Experiments.Count == 0)
            {
                return Result<FeatureDecision>.NullResult(reasons);
            }

            var rolloutRulesLength = rollout.Experiments.Count;
            var rolloutRules = rollout.Experiments;

            var userId = user.GetUserId();
            var attributes = user.GetAttributes();

            var index = 0;
            while (index < rolloutRulesLength)
            {
                // To skip rules
                var skipToEveryoneElse = false;

                //Check forced decision first
                var rule = rolloutRules[index];
                var decisionContext = new OptimizelyDecisionContext(featureFlag.Key, rule.Key);
                var forcedDecisionResponse = ValidatedForcedDecision(decisionContext, config, user);

                reasons += forcedDecisionResponse.DecisionReasons;
                if (forcedDecisionResponse.ResultObject != null)
                {
                    return Result<FeatureDecision>.NewResult(
                        new FeatureDecision(rule, forcedDecisionResponse.ResultObject, null),
                        reasons);
                }

                // Regular decision

                // Get Bucketing ID from user attributes.
                var bucketingIdResult = GetBucketingId(userId, attributes);
                reasons += bucketingIdResult.DecisionReasons;

                var everyoneElse = index == rolloutRulesLength - 1;

                var loggingKey = everyoneElse ? "Everyone Else" : string.Format("{0}", index + 1);

                // Evaluate if user meets the audience condition of this rollout rule
                var doesUserMeetAudienceConditionsResult =
                    ExperimentUtils.DoesUserMeetAudienceConditions(config, rule, user,
                        LOGGING_KEY_TYPE_RULE, rule.Key, Logger);
                reasons += doesUserMeetAudienceConditionsResult.DecisionReasons;
                if (doesUserMeetAudienceConditionsResult.ResultObject)
                {
                    Logger.Log(LogLevel.INFO,
                        reasons.AddInfo(
                            $"User \"{userId}\" meets condition for targeting rule \"{loggingKey}\"."));

                    var bucketedVariation = Bucketer.Bucket(config, rule,
                        bucketingIdResult.ResultObject, userId);
                    reasons += bucketedVariation?.DecisionReasons;

                    if (bucketedVariation?.ResultObject?.Key != null)
                    {
                        Logger.Log(LogLevel.INFO,
                            reasons.AddInfo(
                                $"User \"{userId}\" is in the traffic group of targeting rule \"{loggingKey}\"."));

                        return Result<FeatureDecision>.NewResult(
                            new FeatureDecision(rule, bucketedVariation.ResultObject,
                                FeatureDecision.DECISION_SOURCE_ROLLOUT), reasons);
                    }
                    else if (!everyoneElse)
                    {
                        //skip this logging for everyoneElse rule since this has a message not for everyoneElse
                        Logger.Log(LogLevel.INFO,
                            reasons.AddInfo(
                                $"User \"{userId}\" is not in the traffic group for targeting rule \"{loggingKey}\". Checking EveryoneElse rule now."));
                        skipToEveryoneElse = true;
                    }
                }
                else
                {
                    Logger.Log(LogLevel.DEBUG,
                        reasons.AddInfo(
                            $"User \"{userId}\" does not meet the conditions for targeting rule \"{loggingKey}\"."));
                }

                // the last rule is special for "Everyone Else"
                index = skipToEveryoneElse ? rolloutRulesLength - 1 : index + 1;
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
        public virtual Result<FeatureDecision> GetVariationForFeatureExperiment(
            FeatureFlag featureFlag,
            OptimizelyUserContext user,
            UserAttributes filteredAttributes,
            ProjectConfig config,
            OptimizelyDecideOption[] options,
            UserProfileTracker userProfileTracker = null
        )
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
                Logger.Log(LogLevel.INFO,
                    reasons.AddInfo(
                        $"The feature flag \"{featureFlag.Key}\" is not used in any experiments."));
                return Result<FeatureDecision>.NullResult(reasons);
            }

            foreach (var experimentId in featureFlag.ExperimentIds)
            {
                var experiment = config.GetExperimentFromId(experimentId);
                Variation decisionVariation = null;

                if (string.IsNullOrEmpty(experiment.Key))
                {
                    continue;
                }

                var decisionContext =
                    new OptimizelyDecisionContext(featureFlag.Key, experiment?.Key);
                var forcedDecisionResponse = ValidatedForcedDecision(decisionContext, config, user);

                reasons += forcedDecisionResponse.DecisionReasons;

                if (forcedDecisionResponse?.ResultObject != null)
                {
                    decisionVariation = forcedDecisionResponse.ResultObject;
                }
                else
                {
                    var decisionResponse = GetVariation(experiment, user, config, options,
                        userProfileTracker);

                    reasons += decisionResponse?.DecisionReasons;
                    decisionVariation = decisionResponse.ResultObject;
                }

                if (!string.IsNullOrEmpty(decisionVariation?.Id))
                {
                    Logger.Log(LogLevel.INFO,
                        reasons.AddInfo(
                            $"The user \"{userId}\" is bucketed into experiment \"{experiment.Key}\" of feature \"{featureFlag.Key}\"."));

                    var featureDecision = new FeatureDecision(experiment, decisionVariation,
                        FeatureDecision.DECISION_SOURCE_FEATURE_TEST);
                    return Result<FeatureDecision>.NewResult(featureDecision, reasons);
                }
            }

            Logger.Log(LogLevel.INFO,
                reasons.AddInfo(
                    $"The user \"{userId}\" is not bucketed into any of the experiments on the feature \"{featureFlag.Key}\"."));
            return Result<FeatureDecision>.NullResult(reasons);
        }

        /// <summary>
        /// Get the variation the user is bucketed into for the FeatureFlag
        /// </summary>
        /// <param name="featureFlag">The feature flag the user wants to access.</param>
        /// <param name="user">The user context.</param>
        /// <param name="config">The project config.</param>
        /// <returns>null if the user is not bucketed into any variation or the FeatureDecision entity if the user is
        /// successfully bucketed.</returns>
        public virtual Result<FeatureDecision> GetVariationForFeature(FeatureFlag featureFlag,
            OptimizelyUserContext user, ProjectConfig config
        )
        {
            return GetVariationForFeature(featureFlag, user, config, user.GetAttributes(),
                new OptimizelyDecideOption[] { });
        }

        /// <summary>
        /// Get the decision for a single feature flag, following Swift SDK pattern.
        /// This method processes holdouts, experiments, and rollouts in sequence.
        /// </summary>
        /// <param name="featureFlag">The feature flag to get a decision for.</param>
        /// <param name="user">The user context.</param>
        /// <param name="projectConfig">The project config.</param>
        /// <param name="filteredAttributes">The user's filtered attributes.</param>
        /// <param name="options">Decision options.</param>
        /// <param name="userProfileTracker">User profile tracker for sticky bucketing.</param>
        /// <param name="decideReasons">Decision reasons to merge.</param>
        /// <returns>A decision result for the feature flag.</returns>
        public virtual Result<FeatureDecision> GetDecisionForFlag(
            FeatureFlag featureFlag,
            OptimizelyUserContext user,
            ProjectConfig projectConfig,
            UserAttributes filteredAttributes,
            OptimizelyDecideOption[] options,
            UserProfileTracker userProfileTracker = null,
            DecisionReasons decideReasons = null
        )
        {
            var reasons = new DecisionReasons();
            if (decideReasons != null)
            {
                reasons += decideReasons;
            }

            var userId = user.GetUserId();

            // Check holdouts first (highest priority)
            var holdouts = projectConfig.GetHoldoutsForFlag(featureFlag.Key);
            foreach (var holdout in holdouts)
            {
                var holdoutDecision = GetVariationForHoldout(holdout, user, projectConfig);
                reasons += holdoutDecision.DecisionReasons;

                if (holdoutDecision.ResultObject != null)
                {
                    Logger.Log(LogLevel.INFO,
                        reasons.AddInfo(
                            $"The user \"{userId}\" is bucketed into holdout \"{holdout.Key}\" for feature flag \"{featureFlag.Key}\"."));
                    return Result<FeatureDecision>.NewResult(holdoutDecision.ResultObject, reasons);
                }
            }

            // Check if the feature flag has an experiment and the user is bucketed into that experiment.
            var experimentDecision = GetVariationForFeatureExperiment(featureFlag, user,
                filteredAttributes, projectConfig, options, userProfileTracker);
            reasons += experimentDecision.DecisionReasons;

            if (experimentDecision.ResultObject != null)
            {
                return Result<FeatureDecision>.NewResult(experimentDecision.ResultObject, reasons);
            }

            // Check if the feature flag has rollout and the user is bucketed into one of its rules.
            var rolloutDecision = GetVariationForFeatureRollout(featureFlag, user, projectConfig);
            reasons += rolloutDecision.DecisionReasons;

            if (rolloutDecision.ResultObject != null)
            {
                Logger.Log(LogLevel.INFO,
                    reasons.AddInfo(
                        $"The user \"{userId}\" is bucketed into a rollout for feature flag \"{featureFlag.Key}\"."));
                return Result<FeatureDecision>.NewResult(rolloutDecision.ResultObject, reasons);
            }
            else
            {
                Logger.Log(LogLevel.INFO,
                    reasons.AddInfo(
                        $"The user \"{userId}\" is not bucketed into a rollout for feature flag \"{featureFlag.Key}\"."));
                return Result<FeatureDecision>.NewResult(
                    new FeatureDecision(null, null, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                    reasons);
            }
        }

        public virtual List<Result<FeatureDecision>> GetVariationsForFeatureList(
            List<FeatureFlag> featureFlags,
            OptimizelyUserContext user,
            ProjectConfig projectConfig,
            UserAttributes filteredAttributes,
            OptimizelyDecideOption[] options
        )
        {
            var upsReasons = new DecisionReasons();

            var ignoreUps = options.Contains(OptimizelyDecideOption.IGNORE_USER_PROFILE_SERVICE);
            UserProfileTracker userProfileTracker = null;

            if (UserProfileService != null && !ignoreUps)
            {
                userProfileTracker = new UserProfileTracker(UserProfileService, user.GetUserId(),
                    Logger, ErrorHandler);
                userProfileTracker.LoadUserProfile(upsReasons);
            }

            var decisions = new List<Result<FeatureDecision>>();

            foreach (var featureFlag in featureFlags)
            {
                var decision = GetDecisionForFlag(featureFlag, user, projectConfig, filteredAttributes,
                    options, userProfileTracker, upsReasons);
                decisions.Add(decision);
            }

            if (UserProfileService != null && !ignoreUps &&
                userProfileTracker?.ProfileUpdated == true)
            {
                userProfileTracker.SaveUserProfile();
            }

            return decisions;
        }

        /// <summary>
        /// Get the variation the user is bucketed into for the FeatureFlag
        /// </summary>
        /// <param name="featureFlag">The feature flag the user wants to access.</param>
        /// <param name="user">The user context.</param>
        /// <param name="config">The project config.</param>
        /// <param name="filteredAttributes">The user's attributes. This should be filtered to just attributes in the Datafile.</param>
        /// <param name="options">An array of decision options.</param>
        /// <returns>null if the user is not bucketed into any variation or the FeatureDecision entity if the user is
        /// successfully bucketed.</returns>
        public virtual Result<FeatureDecision> GetVariationForFeature(FeatureFlag featureFlag,
            OptimizelyUserContext user,
            ProjectConfig config,
            UserAttributes filteredAttributes,
            OptimizelyDecideOption[] options
        )
        {
            return GetVariationsForFeatureList(new List<FeatureFlag> { featureFlag },
                    user,
                    config,
                    filteredAttributes,
                    options).
                First();
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
            var bucketingId = userId;

            // If the bucketing ID key is defined in attributes, then use that in place of the userID for the murmur hash key
            if (filteredAttributes != null &&
                filteredAttributes.ContainsKey(ControlAttributes.BUCKETING_ID_ATTRIBUTE))
            {
                if (filteredAttributes[ControlAttributes.BUCKETING_ID_ATTRIBUTE] is string)
                {
                    bucketingId =
                        (string)filteredAttributes[ControlAttributes.BUCKETING_ID_ATTRIBUTE];
                    Logger.Log(LogLevel.DEBUG, $"BucketingId is valid: \"{bucketingId}\"");
                }
                else
                {
                    Logger.Log(LogLevel.WARN,
                        reasons.AddInfo(
                            "BucketingID attribute is not a string. Defaulted to userId"));
                }
            }

            return Result<string>.NewResult(bucketingId, reasons);
        }

        private Result<FeatureDecision> GetVariationForHoldout(
            Holdout holdout,
            OptimizelyUserContext user,
            ProjectConfig config
        )
        {
            var userId = user.GetUserId();
            var reasons = new DecisionReasons();

            if (!holdout.isRunning)
            {
                var infoMessage = $"Holdout \"{holdout.Key}\" is not running.";
                Logger.Log(LogLevel.INFO, infoMessage);
                reasons.AddInfo(infoMessage);
                return Result<FeatureDecision>.NewResult(
                    new FeatureDecision(null, null, FeatureDecision.DECISION_SOURCE_HOLDOUT),
                    reasons
                );
            }

            var audienceResult = ExperimentUtils.DoesUserMeetAudienceConditions(
                config,
                holdout,
                user,
                LOGGING_KEY_TYPE_EXPERIMENT,
                holdout.Key,
                Logger
            );
            reasons += audienceResult.DecisionReasons;

            if (!audienceResult.ResultObject)
            {
                reasons.AddInfo($"User \"{userId}\" does not meet conditions for holdout ({holdout.Key}).");
                return Result<FeatureDecision>.NewResult(
                    new FeatureDecision(null, null, FeatureDecision.DECISION_SOURCE_HOLDOUT),
                    reasons
                );
            }

            var attributes = user.GetAttributes();
            var bucketingIdResult = GetBucketingId(userId, attributes);
            var bucketedVariation = Bucketer.Bucket(config, holdout, bucketingIdResult.ResultObject, userId);
            reasons += bucketedVariation.DecisionReasons;

            if (bucketedVariation.ResultObject != null)
            {
                reasons.AddInfo($"User \"{userId}\" is bucketed into holdout variation \"{bucketedVariation.ResultObject.Key}\".");
                return Result<FeatureDecision>.NewResult(
                    new FeatureDecision(holdout, bucketedVariation.ResultObject, FeatureDecision.DECISION_SOURCE_HOLDOUT),
                    reasons
                );
            }

            reasons.AddInfo($"User \"{userId}\" is not bucketed into holdout variation \"{holdout.Key}\".");

            return Result<FeatureDecision>.NewResult(
                new FeatureDecision(null, null, FeatureDecision.DECISION_SOURCE_HOLDOUT),
                reasons
            );
        }
        /// <summary>
        /// Finds a validated forced decision.
        /// </summary>
        /// <param name="context">Object containing flag and rule key of which forced decision is set.</param>
        /// <param name="config">The Project config.</param>
        /// <param name="user">Optimizely user context.</param>
        /// <returns>A result with the variation</returns>
        public Result<Variation> ValidatedForcedDecision(OptimizelyDecisionContext context,
            ProjectConfig config, OptimizelyUserContext user
        )
        {
            var reasons = new DecisionReasons();
            var userId = user.GetUserId();
            var forcedDecision = user.GetForcedDecision(context);
            if (config != null && forcedDecision != null)
            {
                var loggingKey = context.RuleKey != null ?
                    "flag (" + context.FlagKey + "), rule (" + context.RuleKey + ")" :
                    "flag (" + context.FlagKey + ")";
                var variationKey = forcedDecision.VariationKey;
                var variation = config.GetFlagVariationByKey(context.FlagKey, variationKey);
                if (variation != null)
                {
                    reasons.AddInfo("Decided by forced decision.");
                    reasons.AddInfo(
                        "Variation ({0}) is mapped to {1} and user ({2}) in the forced decision map.",
                        variationKey, loggingKey, userId);
                    return Result<Variation>.NewResult(variation, reasons);
                }
                else
                {
                    reasons.AddInfo(
                        "Invalid variation is mapped to {0} and user ({1}) in the forced decision map.",
                        loggingKey, userId);
                }
            }

            return Result<Variation>.NullResult(reasons);
        }
    }
}
