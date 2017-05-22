/* 
 * Copyright 2017, Optimizely
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

using Attribute = OptimizelySDK.Entity.Attribute;

namespace OptimizelySDK
{
    public class Decision
    {

        /// <summary>
        /// The ID of the {@link com.optimizely.ab.config.Variation} the user was bucketed into.
        /// </summary>
        public string VariationId;

        /// <summary>
        /// Initialize a Decision object.
        /// </summary>
        /// <param name="variationId">The ID of the variation the user was bucketed into.</param>
        public Decision(string variationId)
        {
            this.VariationId = variationId;
        }

        public int HashCode()
        {
            return VariationId.GetHashCode();
        }

        public Dictionary<string, string> ToMap()
        {
            Dictionary<string, string> decisionMap = new Dictionary<string, string>();

            decisionMap[UserProfileService.VARIATION_ID_KEY] = VariationId;

            return decisionMap;
        }
    }

    /// <summary>
    /// Optimizely's decision service that determines which variation of an experiment the user will be allocated to.
    /// The decision service contains all logic around how a user decision is made.This includes all of the following:
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


        /**
         * Initialize a decision service for the Optimizely client.
         * @param bucketer Base bucketer to allocate new users to an experiment.
         * @param errorHandler The error handler of the Optimizely client.
         * @param projectConfig Optimizely Project Config representing the datafile.
         * @param userProfileService UserProfileService implementation for storing user info.
         */
        public DecisionService(Bucketer bucketer,
                               IErrorHandler errorHandler,
                               ProjectConfig projectConfig,
                               UserProfileService userProfileService,
                               ILogger logger)
        {
            this.Bucketer = bucketer;
            this.ErrorHandler = errorHandler;
            this.ProjectConfig = projectConfig;
            this.UserProfileService = userProfileService;
            this.Logger = logger;
        }

        /// <summary>
        /// Get a { @link Variation } of an { @link Experiment } for a user to be allocated into.
        /// </summary>
        /// <param name="experiment">The Experiment the user will be bucketed into.</param>
        /// <param name="userId">The userId of the user.
        /// <param name="filteredAttributes">The user's attributes. This should be filtered to just attributes in the Datafile.</param>
        /// <returns>The { @link Variation} the user is allocated into.</returns>
        public Variation GetVariation(Experiment experiment,
                                                string userId,
                                                UserAttributes filteredAttributes)
        {

            if (!experiment.IsExperimentRunning)
            {
                return new Variation();
            }

            // check for whitelisting
            Variation variation = GetWhitelistedVariation(experiment, userId);
            if (variation != null)
            {
                return variation;
            }

            // fetch the user profile map from the user profile service
            UserProfile userProfile = null;
            if (UserProfileService != null)
            {
                try
                {
                    Dictionary<string, object> userProfileMap = UserProfileService.Lookup(userId);
                    if (userProfileMap == null)
                    {
                        Logger.Log(LogLevel.INFO, "We were unable to get a user profile map from the UserProfileService.");
                    }
                    else if (UserProfileUtils.IsValidUserProfileMap(userProfileMap))
                    {
                        userProfile = UserProfileUtils.ConvertMapToUserProfile(userProfileMap);
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

            // check if user exists in user profile
            if (userProfile != null)
            {
                variation = GetStoredVariation(experiment, userProfile);
                // return the stored variation if it exists
                if (variation != null)
                {
                    return variation;
                }
            }
            else
            { // if we could not find a user profile, make a new one
                userProfile = new UserProfile(userId, new Dictionary<string, Decision>());
            }

            if (Validator.IsUserInExperiment(ProjectConfig, experiment, filteredAttributes))
            {
                variation = Bucketer.Bucket(ProjectConfig, experiment, userId);

                if (variation != null)
                {
                    if (UserProfileService != null)
                    {
                        SaveVariation(experiment, variation, userProfile);
                    }
                    else
                    {
                        Logger.Log(LogLevel.INFO, "This decision will not be saved since the UserProfileService is null.");
                    }
                }

                return variation;

            }
            else
            {
                Logger.Log(LogLevel.INFO, string.Format("User \"{0}\" does not meet conditions to be in experiment \"{1}\".", userId, experiment.Key));
            }

            return new Variation();
        }

        /**
         * Get the variation the user has been whitelisted into.
         * @param experiment {@link Experiment} in which user is to be bucketed.
         * @param userId User Identifier
         * @return null if the user is not whitelisted into any variation
         *      {@link Variation} the user is bucketed into if the user has a specified whitelisted variation.
         */
        public Variation GetWhitelistedVariation(Experiment experiment, string userId)
        {
            // if a user has a forced variation mapping, return the respective variation
            experiment.GenerateKeyMap();
            Dictionary<string, string> userIdToVariationKeyMap = experiment.UserIdToKeyVariations;

            if (userIdToVariationKeyMap.ContainsKey(userId))
            {
                string forcedVariationKey = userIdToVariationKeyMap[userId];
                Variation forcedVariation = experiment.VariationKeyToVariationMap.ContainsKey(forcedVariationKey) 
                    ? experiment.VariationKeyToVariationMap[forcedVariationKey] : null;
                if (forcedVariation != null)
                {
                    Logger.Log(LogLevel.INFO, string.Format("User \"{0}\" is forced in variation \"{1}\".", userId, forcedVariationKey));
                }
                else
                {
                    Logger.Log(LogLevel.ERROR, string.Format("Variation \"{0}\" is not in the datafile. Not activating user \"{1}\".", forcedVariationKey, userId));
                }

                return forcedVariation;
            }

            return null;
        }

        /// <summary>
        /// Get the { @link Variation } that has been stored for the user in the {@link UserProfileService} implementation.
        /// </summary>
        /// <param name="experiment">which the user was bucketed</param>
        /// <param name="userProfile">User profile of the user</param>
        /// <returns>The user was previously bucketed into.</returns>
        public Variation GetStoredVariation(Experiment experiment,
                                               UserProfile userProfile)
        {
            // ---------- Check User Profile for Sticky Bucketing ----------
            // If a user profile instance is present then check it for a saved variation
            string experimentId = experiment.Id;
            string experimentKey = experiment.Key;

            Decision decision = userProfile.ExperimentBucketMap.ContainsKey(experimentId) ? 
                userProfile.ExperimentBucketMap[experimentId] : null;

            if (decision != null)
            {
                try
                {
                    string variationId = decision.VariationId;

                    Variation savedVariation = ProjectConfig
                            .ExperimentIdMap[experimentId].VariationIdToVariationMap.ContainsKey(variationId) ?
                            ProjectConfig.ExperimentIdMap[experimentId].VariationIdToVariationMap[variationId] : null; 

                    if (savedVariation != null)
                    {
                        Logger.Log(LogLevel.INFO, string.Format("Returning previously activated variation \"{0}\" of experiment \"{1}\" for user \"{2}\" from user profile.",
                        savedVariation.Key, experimentKey, userProfile.UserId));
                        return savedVariation;
                    }
                    else
                    {
                        Logger.Log(LogLevel.INFO, string.Format("User \"{0}\" was previously bucketed into variation with ID \"{1}\" for experiment \"{2}\", but no matching variation was found for that user. We will re-bucket the user.", 
                            userProfile.UserId, variationId, experimentId));

                        return null;
                    }
                }
                catch(Exception ex)
                {
                    return null;
                }
            }
            else
            {
                Logger.Log(LogLevel.INFO, string.Format("No previously activated variation of experiment \"{0}\" for user \"{1}\" found in user profile.", experimentKey, userProfile.UserId));

                return null;
            }
        }

        /**
         * Save a {@link Variation} of an {@link Experiment} for a user in the {@link UserProfileService}.
         *
         * @param experiment The experiment the user was buck
         * @param variation The Variation to save.
         * @param userProfile A {@link UserProfile} instance of the user information.
         */
        public void SaveVariation(Experiment experiment,
                           Variation variation,
                           UserProfile userProfile)
        {
            // only save if the user has implemented a user profile service
            if (UserProfileService != null)
            {
                String experimentId = experiment.Id;
                String variationId = variation.Id;
                Decision decision;
                if (userProfile.ExperimentBucketMap.ContainsKey(experimentId))
                {
                    decision = userProfile.ExperimentBucketMap[experimentId];
                    decision.VariationId = variationId;
                }
                else
                {
                    decision = new Decision(variationId);
                }
                userProfile.ExperimentBucketMap[experimentId] = decision;

                try
                {
                    UserProfileService.Save(userProfile.ToMap());
                    Logger.Log(LogLevel.INFO, string.Format("Saved variation \"{0}\" of experiment \"{1}\" for user \"{2}\".",
                    variationId, experimentId, userProfile.UserId));

                }
                catch (Exception exception)
                {
                    Logger.Log(LogLevel.ERROR, string.Format("Failed to save variation \"{0}\" of experiment \"{1}\" for user \"{2}\".",
                    variationId, experimentId, userProfile.UserId));
                    ErrorHandler.HandleError(new Exceptions.OptimizelyRuntimeException(exception.Message));
                }
            }
        }
    }
}