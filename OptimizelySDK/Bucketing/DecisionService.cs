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

using System;
using System.Collections.Generic;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
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
        private Bucketer Bucketer;
        private IErrorHandler ErrorHandler;
        private ProjectConfig ProjectConfig;
        private UserProfileService UserProfileService;
        private ILogger Logger;
        
        /// <summary>
        ///  Initialize a decision service for the Optimizely client.
        /// </summary>
        /// <param name = "bucketer" > Base bucketer to allocate new users to an experiment.</param>
        /// <param name = "errorHandler" > The error handler of the Optimizely client.</param>
        /// <param name = "projectConfig" > Optimizely Project Config representing the datafile.</param>
        /// <param name = "userProfileService" ></ param >
        /// < param name= "logger" > UserProfileService implementation for storing user info.</param>
        public DecisionService(Bucketer bucketer, IErrorHandler errorHandler, ProjectConfig projectConfig, UserProfileService userProfileService, ILogger logger)
        {
            Bucketer = bucketer;
            ErrorHandler = errorHandler;
            ProjectConfig = projectConfig;
            UserProfileService = userProfileService;
            Logger = logger;
        }

        /// <summary>
        /// Get a Variation of an Experiment for a user to be allocated into.
        /// </summary>
        /// <param name = "experiment" > The Experiment the user will be bucketed into.</param>
        /// <param name = "userId" > The userId of the user.
        /// <param name = "filteredAttributes" > The user's attributes. This should be filtered to just attributes in the Datafile.</param>
        /// <returns>The Variation the user is allocated into.</returns>
        public virtual Variation GetVariation(Experiment experiment, string userId, UserAttributes filteredAttributes)
        {
            if (!ExperimentUtils.IsExperimentActive(experiment, Logger)) return null;

            // check if a forced variation is set
            var forcedVariation = ProjectConfig.GetForcedVariation(experiment.Key, userId);
            if (forcedVariation != null)
                return forcedVariation;

            var variation = GetWhitelistedVariation(experiment, userId);

            if (variation != null)   return variation;
            UserProfile userProfile = null;
            if (UserProfileService != null)
            {
                try
                {
                    Dictionary<string, object> userProfileMap = UserProfileService.Lookup(userId);
                    if (userProfileMap != null && UserProfileUtil.IsValidUserProfileMap(userProfileMap))
                    {
                        userProfile = UserProfileUtil.ConvertMapToUserProfile(userProfileMap);
                        variation = GetStoredVariation(experiment, userProfile);
                        if (variation != null) return variation;
                    }
                    else if (userProfileMap == null)
                    {
                        Logger.Log(LogLevel.INFO, "We were unable to get a user profile map from the UserProfileService.");
                    }
                    else
                    {
                        Logger.Log(LogLevel.ERROR, "The UserProfileService returned an invalid map.");
                    }
                }
                catch (Exception exception)
                {
                    Logger.Log(LogLevel.ERROR, exception.Message);
                    ErrorHandler.HandleError(new Exceptions.OptimizelyRuntimeException(exception.Message));
                }
            }

            if (ExperimentUtils.IsUserInExperiment(ProjectConfig, experiment, filteredAttributes, Logger))
            {
                // Get Bucketing ID from user attributes.
                string bucketingId = GetBucketingId(userId, filteredAttributes);

                variation = Bucketer.Bucket(ProjectConfig, experiment, bucketingId, userId);

                if (variation != null && variation.Key != null)
                {
                    if (UserProfileService != null)
                    {
                        var bucketerUserProfile = userProfile ?? new UserProfile(userId, new Dictionary<string, Decision>());
                        SaveVariation(experiment, variation, bucketerUserProfile);

                    }
                    else
                        Logger.Log(LogLevel.INFO, "This decision will not be saved since the UserProfileService is null.");
                 }

                return variation;
            }
            Logger.Log(LogLevel.INFO, string.Format("User \"{0}\" does not meet conditions to be in experiment \"{1}\".", userId, experiment.Key));

            return null;
        }

        /// <summary>
        /// Get the variation the user has been whitelisted into.
        /// </summary>
        /// <param name = "experiment" >in which user is to be bucketed.</param>
        /// <param name = "userId" > User Identifier</param>
        /// <returns>if the user is not whitelisted into any variation {@link Variation}
        /// the user is bucketed into if the user has a specified whitelisted variation.</returns>
        public Variation GetWhitelistedVariation(Experiment experiment, string userId)
        {
            //if a user has a forced variation mapping, return the respective variation
            Dictionary<string, string> userIdToVariationKeyMap = experiment.UserIdToKeyVariations;

            if (!userIdToVariationKeyMap.ContainsKey(userId))
                return null;

            string forcedVariationKey = userIdToVariationKeyMap[userId];
            Variation forcedVariation = experiment.VariationKeyToVariationMap.ContainsKey(forcedVariationKey)
                ? experiment.VariationKeyToVariationMap[forcedVariationKey] 
                : null;

            if (forcedVariation != null)
                Logger.Log(LogLevel.INFO, string.Format("User \"{0}\" is forced in variation \"{1}\".", userId, forcedVariationKey));
            else
                Logger.Log(LogLevel.ERROR, string.Format("Variation \"{0}\" is not in the datafile. Not activating user \"{1}\".", forcedVariationKey, userId));

            return forcedVariation;
        }

        /// <summary>
        /// Get the { @link Variation } that has been stored for the user in the { @link UserProfileService } implementation.
        /// </summary>
        /// <param name = "experiment" > which the user was bucketed</param>
        /// <param name = "userProfile" > User profile of the user</param>
        /// <returns>The user was previously bucketed into.</returns>
        public Variation GetStoredVariation(Experiment experiment, UserProfile userProfile)
        {
            // ---------- Check User Profile for Sticky Bucketing ----------
            // If a user profile instance is present then check it for a saved variation
            string experimentId = experiment.Id;
            string experimentKey = experiment.Key;

            Decision decision = userProfile.ExperimentBucketMap.ContainsKey(experimentId) ?
            userProfile.ExperimentBucketMap[experimentId] : null;

            if (decision == null)
            {
                Logger.Log(LogLevel.INFO, string.Format("No previously activated variation of experiment \"{0}\" for user \"{1}\" found in user profile.", experimentKey, userProfile.UserId));
                return null;
            }

            try
            {
                string variationId = decision.VariationId;

                Variation savedVariation = ProjectConfig.ExperimentIdMap[experimentId].VariationIdToVariationMap.ContainsKey(variationId) 
                    ? ProjectConfig.ExperimentIdMap[experimentId].VariationIdToVariationMap[variationId] 
                    : null;

                if (savedVariation == null)
                {
                    Logger.Log(LogLevel.INFO, string.Format("User \"{0}\" was previously bucketed into variation with ID \"{1}\" for experiment \"{2}\", but no matching variation was found for that user. We will re-bucket the user.",
                    userProfile.UserId, variationId, experimentId));
                    return null;
                }

                Logger.Log(LogLevel.INFO, string.Format("Returning previously activated variation \"{0}\" of experiment \"{1}\" for user \"{2}\" from user profile.",
                savedVariation.Key, experimentKey, userProfile.UserId));
                return savedVariation;
            }
            catch (Exception)
            {
                return null;
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
                Logger.Log(LogLevel.INFO, string.Format("Saved variation \"{0}\" of experiment \"{1}\" for user \"{2}\".",
                    variation.Id, experiment.Id, userProfile.UserId));
            }
            catch (Exception exception)
            {
                Logger.Log(LogLevel.ERROR, string.Format("Failed to save variation \"{0}\" of experiment \"{1}\" for user \"{2}\".",
                    variation.Id, experiment.Id, userProfile.UserId));
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
        /// <returns>null if the user is not bucketed into the rollout or if the feature flag was not attached to a rollout.
        /// otherwise the FeatureDecision entity</returns>
        public virtual FeatureDecision GetVariationForFeatureRollout(FeatureFlag featureFlag, string userId, UserAttributes filteredAttributes)
        {
            if (featureFlag == null)
            {
                Logger.Log(LogLevel.ERROR, "Invalid feature flag provided.");
                return null;
            }

            if (string.IsNullOrEmpty(featureFlag.RolloutId))
            {
                Logger.Log(LogLevel.INFO, $"The feature flag \"{featureFlag.Key}\" is not used in a rollout.");
                return null;
            }

            Rollout rollout = ProjectConfig.GetRolloutFromId(featureFlag.RolloutId);

            if (string.IsNullOrEmpty(rollout.Id))
            {
                Logger.Log(LogLevel.ERROR, $"The rollout with id \"{featureFlag.RolloutId}\" is not found in the datafile for feature flag \"{featureFlag.Key}\"");
                return null;
            }

            Variation variation = null;
            var rolloutRulesLength = rollout.Experiments.Count;
            
            // Get Bucketing ID from user attributes.
            string bucketingId = GetBucketingId(userId, filteredAttributes);

            // For all rules before the everyone else rule
            for (int i=0; i < rolloutRulesLength - 1; i++)
            {
                var rolloutRule = rollout.Experiments[i];
                if (ExperimentUtils.IsUserInExperiment(ProjectConfig, rolloutRule, filteredAttributes, Logger))
                {
                    variation = Bucketer.Bucket(ProjectConfig, rolloutRule, bucketingId, userId);
                    if (variation == null || string.IsNullOrEmpty(variation.Id))
                        break;

                    return new FeatureDecision(rolloutRule, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);
                }
                else
                {
                    var audience = ProjectConfig.GetAudience(rolloutRule.AudienceIds[0]);
                    Logger.Log(LogLevel.DEBUG, $"User \"{userId}\" does not meet the conditions to be in rollout rule for audience \"{audience.Name}\".");
                }
            }

            // Get the last rule which is everyone else rule.
            var everyoneElseRolloutRule = rollout.Experiments[rolloutRulesLength - 1];
            if (ExperimentUtils.IsUserInExperiment(ProjectConfig, everyoneElseRolloutRule, filteredAttributes, Logger))
            {
                variation = Bucketer.Bucket(ProjectConfig, everyoneElseRolloutRule, bucketingId, userId);
                if (variation != null && !string.IsNullOrEmpty(variation.Id))
                    return new FeatureDecision(everyoneElseRolloutRule, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);
            }
            else
            {
                var audience = ProjectConfig.GetAudience(everyoneElseRolloutRule.AudienceIds[0]);
                Logger.Log(LogLevel.DEBUG, $"User \"{userId}\" does not meet the conditions to be in rollout rule for audience \"{audience.Name}\".");
            }

            return null;
        }

        /// <summary>
        /// Get the variation if the user is bucketed for one of the experiments on this feature flag.
        /// </summary>
        /// <param name = "featureFlag" >The feature flag the user wants to access.</param>
        /// <param name = "userId" >User Identifier</param>
        /// <param name = "filteredAttributes" >The user's attributes. This should be filtered to just attributes in the Datafile.</param>
        /// <returns>null if the user is not bucketed into the rollout or if the feature flag was not attached to a rollout.
        /// Otherwise the FeatureDecision entity</returns>
        public virtual FeatureDecision GetVariationForFeatureExperiment(FeatureFlag featureFlag, string userId, UserAttributes filteredAttributes)
        {
            if (featureFlag == null)
            {
                Logger.Log(LogLevel.ERROR, "Invalid feature flag provided.");
                return null;
            }

            if (featureFlag.ExperimentIds == null || featureFlag.ExperimentIds.Count == 0)
            {
                Logger.Log(LogLevel.INFO, $"The feature flag \"{featureFlag.Key}\" is not used in any experiments.");
                return null;
            }

            foreach (var experimentId in featureFlag.ExperimentIds)
            {
                var experiment = ProjectConfig.GetExperimentFromId(experimentId);

                if (string.IsNullOrEmpty(experiment.Key))
                    continue;

                var variation = GetVariation(experiment, userId, filteredAttributes);

                if (variation != null && !string.IsNullOrEmpty(variation.Id))
                {
                    Logger.Log(LogLevel.INFO, $"The user \"{userId}\" is bucketed into experiment \"{experiment.Key}\" of feature \"{featureFlag.Key}\".");
                    return new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_EXPERIMENT);
                }
            }

            Logger.Log(LogLevel.INFO, $"The user \"{userId}\" is not bucketed into any of the experiments on the feature \"{featureFlag.Key}\".");
            return null;
        }

        /// <summary>
        /// Get the variation the user is bucketed into for the FeatureFlag
        /// </summary>
        /// <param name = "featureFlag" >The feature flag the user wants to access.</param>
        /// <param name = "userId" >User Identifier</param>
        /// <param name = "filteredAttributes" >The user's attributes. This should be filtered to just attributes in the Datafile.</param>
        /// <returns>null if the user is not bucketed into any variation or the FeatureDecision entity if the user is 
        /// successfully bucketed.</returns>
        public virtual FeatureDecision GetVariationForFeature(FeatureFlag featureFlag, string userId, UserAttributes filteredAttributes)
        {
            // Check if the feature flag has an experiment and the user is bucketed into that experiment.
            var decision = GetVariationForFeatureExperiment(featureFlag, userId, filteredAttributes);

            if (decision != null)
                return decision;

            // Check if the feature flag has rollout and the the user is bucketed into one of its rules.
            decision = GetVariationForFeatureRollout(featureFlag, userId, filteredAttributes);

            if (decision != null)
                Logger.Log(LogLevel.INFO, $"The user \"{userId}\" is bucketed into a rollout for feature flag \"{featureFlag.Key}\".");
            else
                Logger.Log(LogLevel.INFO, $"The user \"{userId}\" is not bucketed into a rollout for feature flag \"{featureFlag.Key}\".");

            return decision;
        }

        /// <summary>
        /// Get Bucketing ID from user attributes.
        /// </summary>
        /// <param name = "userId" >User Identifier</param>
        /// <param name = "filteredAttributes" >The user's attributes.</param>
        /// <returns>Bucketing Id if it is a string type in attributes, user Id otherwise.</returns>
        private string GetBucketingId(string userId, UserAttributes filteredAttributes)
        {
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
                    Logger.Log(LogLevel.WARN, "BucketingID attribute is not a string. Defaulted to userId");
                }
            }

            return bucketingId;
        }
    }
}