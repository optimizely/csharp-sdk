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
    public class DecisionService
    {
        private readonly Bucketer Bucketer;
        private readonly ProjectConfig ProjectConfig;
        private readonly UserProfileService UserProfile;
        private ILogger Logger { get; set; }
		
		/// <summary>
		/// Initialize a decision service for the Optimizely client.
		/// </summary>
		/// <param name="bucketer"> Base bucketer to allocate new users to an experiment.</param>
		/// <param name="projectConfig"> Optimizely Project Config representing the datafile.</param>
		/// <param name="userProfile"> UserProfile implementation for storing decisions.</param>
		public DecisionService(Bucketer bucketer,
						   ProjectConfig projectConfig,
                           UserProfileService userProfile,
                           ILogger logger)
		{
			this.Bucketer = bucketer;
			this.ProjectConfig = projectConfig;
			this.UserProfile = userProfile;
            this.Logger = logger;
		}

		/**
		 * Get a {@link Variation} of an {@link Experiment} for a user to be allocated into.
		 *
		 * @param experiment The Experiment the user will be bucketed into.
		 * @param userId The userId of the user.
		 * @param filteredAttributes The user's attributes. This should be filtered to just attributes in the Datafile.
		 * @return The {@link Variation} the user is allocated into.
		 */

		public Variation GetVariation(Experiment experiment,
												string userId,
												UserAttributes filteredAttributes)
		{

            if (!experiment.IsExperimentRunning)
			{
				return null;
			}

			Variation variation;

			// check for whitelisting
			variation = GetWhitelistedVariation(experiment, userId);
			if (variation != null)
			{
				return variation;
			}

			// check if user exists in user profile
			variation = GetStoredVariation(experiment, userId);
			if (variation != null)
			{
				return variation;
			}

			if (Validator.IsUserInExperiment(ProjectConfig, experiment, filteredAttributes))
			{
				Variation bucketedVariation = Bucketer.Bucket(ProjectConfig,experiment, userId);

				if (bucketedVariation != null)
				{
					storeVariation(experiment, bucketedVariation, userId);
				}

				return bucketedVariation;
			}

            Logger.Log(LogLevel.INFO, string.Format("User \"{0}\" does not meet conditions to be in experiment \"{1}\".", userId, experiment.Id));

            return null;
		}

		/**
		 * Get the variation the user has been whitelisted into.
		 * @param experiment {@link Experiment} in which user is to be bucketed.
		 * @param userId User Identifier
		 * @return null if the user is not whitelisted into any variation
		 *      {@link Variation} the user is bucketed into if the user has a specified whitelisted variation.
		 */
		Variation GetWhitelistedVariation(Experiment experiment, String userId)
		{
            // if a user has a forced variation mapping, return the respective variation
            Dictionary<string, string> userIdToVariationKeyMap = experiment.UserIdToKeyVariations;
			
            if (userIdToVariationKeyMap.ContainsKey(userId))
			{
                string forcedVariationKey = userIdToVariationKeyMap[userId];

                Variation forcedVariation = experiment.VariationKeyToVariationMap[forcedVariationKey];
				
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

		/**
		 * Get the {@link Variation} that has been stored for the user in the {@link UserProfile} implementation.
		 * @param experiment {@link Experiment} in which the user was bucketed.
		 * @param userId User Identifier
		 * @return null if the {@link UserProfile} implementation is null or the user was not previously bucketed.
		 *      else return the {@link Variation} the user was previously bucketed into.
		 */

        string GetVariationIdFromUserProfile(Dictionary<string, object> userProfile, string experimentId)
        {
            var decisions = userProfile[UserProfileService.DECISIONS_KEY];

            if(decisions != null && decisions is Dictionary<string, object>)
            {
                var experimentVariations = (Dictionary<string, object>)decisions;
                var variations = experimentVariations[experimentId];
                if(variations != null && variations is Dictionary<string, object>)
                {
                    var variationsDict = (Dictionary<string, object>)variations;
                    return variationsDict[UserProfileService.VARIATION_ID_KEY].ToString();
                }
            }

            return null;
        }
		Variation GetStoredVariation(Experiment experiment, string userId)
		{
            // ---------- Check User Profile for Sticky Bucketing ----------
            // If a user profile instance is present then check it for a saved variation
            string experimentId = experiment.Id;
            string experimentKey = experiment.Key;
			
            if (UserProfile != null)
			{
                //TODO: Find variation ID based on experiment ID
                var variationId = GetVariationIdFromUserProfile(UserProfile.Lookup(userId), experimentId);
				
                if (variationId != null)
				{
                    Variation savedVariation = ProjectConfig.ExperimentIdMap[experimentId].VariationIdToVariationMap[variationId];

					Logger.Log(LogLevel.INFO, string.Format("Returning previously activated variation \"{0}\" of experiment \"{1}\" "
					      + "for user \"{}\" from user profile.",
					savedVariation.Key, experimentKey, userId
					));

					// A variation is stored for this combined bucket id
					return savedVariation;
				}
				else
				{

					Logger.Log(LogLevel.INFO, string.Format("No previously activated variation of experiment \"{0}\" for user \"{1}\" found in user profile.",
                            experimentKey, userId));
				}
			}

			return null;
		}

		/**
		 * Store a {@link Variation} of an {@link Experiment} for a user in the {@link UserProfile}.
		 *
		 * @param experiment The experiment the user was buck
		 * @param variation The Variation to store.
		 * @param userId The ID of the user.
		 */
		void storeVariation(Experiment experiment, Variation variation, String userId)
		{
			String experimentId = experiment.Id;
			
            // ---------- Save Variation to User Profile ----------
			// If a user profile is present give it a variation to store
			if (UserProfile != null)
			{
                string bucketedVariationId = variation.Id;

				UserProfile.Save(UserProfile.ToMap());
				Logger.Log(LogLevel.INFO, string.Format("Saved variation \"{0}\" of experiment \"{1}\" for user \"{2}\".",
                                                        bucketedVariationId, experimentId, userId));
			}
		}
    }
}
