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

using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.Bucketing
{
    public class UserProfileUtil
    {
        /// <summary>
        /// Validate whether a Dictionary<String, Object> can be transformed into a UserProfile.
        /// </summary>
        /// <param name="map"The map to check.></param>
        /// <returns>True if the map can be converted into a UserProfile. False if the map cannot be converted.</returns>
        public static bool IsValidUserProfileMap(Dictionary<string, object> map)
        {
            // The Map must contain a value for the user ID and experiment bucket map
            if (!map.ContainsKey(UserProfile.USER_ID_KEY) ||
                !map.ContainsKey(UserProfile.EXPERIMENT_BUCKET_MAP_KEY))
                return false;

            // the map is good enough for us to use
            return true;
        }

        /// <summary>
        /// Convert a Map to a UserProfile instance.
        /// </summary>
        /// <param name="map">The map to construct the UserProfile</param>
        /// <returns>A UserProfile instance.</returns>
        public static UserProfile ConvertMapToUserProfile(Dictionary<string, object> map)
        {
            var experimentBucketMap = (Dictionary<string, Dictionary<string, string>>)map[UserProfile.EXPERIMENT_BUCKET_MAP_KEY];
            Dictionary<string, Decision> decisions = experimentBucketMap.ToDictionary(
                keySelector: kvp => kvp.Key, 
                elementSelector: kvp => new Decision(kvp.Value[UserProfile.VARIATION_ID_KEY]));

            return new UserProfile(
                userId: (string)map[UserProfile.USER_ID_KEY], 
                experimentBucketMap: decisions);
        }
    }
}