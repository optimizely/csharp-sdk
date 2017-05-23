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

namespace OptimizelySDK.Utils
{
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
                if (experimentBucketMap.Values.Count == 0) return false;
            }
            catch (Exception e)
            {
                return false;
            }

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

            foreach (var entry in experimentBucketMap)
            {
                Decision decision = new Decision(entry.Value[UserProfileService.VARIATION_ID_KEY]);
                decisions[entry.Key] = decision;
            }
            return new UserProfile(userId, decisions);
        }
    }
}
