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

namespace OptimizelySDK
{

    public class UserProfile
    {
        /// <summary>
        /// A user's ID.
        /// </summary>
        public string UserId;
        /// <summary>
        /// The bucketing experimentBucketMap of the user.
        /// </summary>
        public Dictionary<string, Decision> ExperimentBucketMap;

        /// <summary>
        /// Construct a User Profile instance from explicit components.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="experimentBucketMap">The bucketing experimentBucketMap of the user.</param>
        public UserProfile(string userId, Dictionary<string, Decision> experimentBucketMap)
        {
            this.UserId = userId;
            this.ExperimentBucketMap = experimentBucketMap;
        }

        public int HashCode()
            {
                int result = UserId.GetHashCode();
                result = 31 * result + ExperimentBucketMap.GetHashCode();
                return result;
        }


        /// <summary>
        /// Convert a User Profile instance to a Map.
        /// </summary>
        /// <returns>A map representation of the user profile instance.</returns>
        public Dictionary<string, object> ToMap()
        {
            Dictionary<string, object> userProfileMap = new Dictionary<string, object>();

            userProfileMap[UserProfileService.USER_ID_KEY] = UserId;

            var decisionsMap = new Dictionary<string, Dictionary<string, string>>();
            //var decisionMap = new Dictionary<string, object>();
            
            foreach(var row in ExperimentBucketMap)
            {
                decisionsMap[row.Key] = row.Value.ToMap();
            }

            userProfileMap[UserProfileService.EXPERIMENT_BUCKET_MAP_KEY] = decisionsMap;

            return userProfileMap;
        }
    }


    /// <summary>
    /// Class encapsulating user profile service functionality.
    /// Override with your own implementation for storing and retrieving the user profile.
    /// </summary>

    public abstract class UserProfileService
    {
        /** The key for the user ID. Returns a String.*/
        public const string USER_ID_KEY = "user_id";
        /** The key for the decisions Map. Returns a {@code Map<String, Map<String, String>>}.*/
        public const string EXPERIMENT_BUCKET_MAP_KEY = "experiment_bucket_map";
        /** The key for the variation Id within a decision Map. */
        public const string VARIATION_ID_KEY = "variation_id";

		public abstract Dictionary<string, object> Lookup(String userId);

        public abstract void Save(Dictionary<string, object> userProfile);
    }

    public class UserProfileUtils
    {
        
        /// <summary>
        /// Validate whether a {@code Map<String, Object>} can be transformed into a {@link UserProfile}.
        /// </summary>
        /// <param name="map"The map to check.></param>
        /// <returns>True if the map can be converted into a {@link UserProfile}. False if the map cannot be converted.</returns>
        public static bool IsValidUserProfileMap(Dictionary<string, object> map)
        {
            // The Map must contain a value for the user ID
            if (!map.ContainsKey(UserProfileService.USER_ID_KEY))
            {
                return false;
            }
            // The Map must contain a value for the experiment bucket map
            if (!map.ContainsKey(UserProfileService.EXPERIMENT_BUCKET_MAP_KEY))
            {
                return false;
            }
            // The value for the experimentBucketMapKey must be a map
            //if (!(map[UserProfileService.EXPERIMENT_BUCKET_MAP_KEY] is Dictionary<string, object>))
            //{
            //    return false;
            //}
            // Try and cast the experimentBucketMap value to a typed map
            Dictionary<string, Dictionary<string, string>> experimentBucketMap;
            try
            {
                experimentBucketMap = (Dictionary<string, Dictionary<string, string>>)map[UserProfileService.EXPERIMENT_BUCKET_MAP_KEY];
            }
            catch (Exception e)
            {
                return false;
            }

            //// Check each Decision in the map to make sure it has a variation Id Key
            //foreach(var decision in experimentBucketMap)
            //{
            //    if(!decision.Key))
            //}
            foreach (Dictionary<string, string> decision in experimentBucketMap.Values)
            {
                if (!decision.ContainsKey(UserProfileService.VARIATION_ID_KEY))
                {
                    return false;
                }
            }

            // the map is good enough for us to use
            return true;
        }

        

        /// <summary>
        /// Convert a Map to a {@link UserProfile} instance.
        /// </summary>
        /// <param name="map">The map to construct the { @link UserProfile }</param>
        /// <returns>A {@link UserProfile}instance.</returns>
        public static UserProfile ConvertMapToUserProfile(Dictionary<string, object> map)
        {
            string userId = (string)map[UserProfileService.USER_ID_KEY];
            
            var experimentBucketMap = (Dictionary<string, Dictionary<string, string>>)map[UserProfileService.EXPERIMENT_BUCKET_MAP_KEY];

            Dictionary<string, Decision> decisions = new Dictionary<string, Decision>();

            foreach(var entry in experimentBucketMap)
            {
                Decision decision = new Decision(entry.Value[UserProfileService.VARIATION_ID_KEY]);
                decisions[entry.Key] = decision;
            }
            return new UserProfile(userId, decisions);
        }
    }

}
