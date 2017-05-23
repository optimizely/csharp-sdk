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

            foreach (var row in ExperimentBucketMap)
            {
                decisionsMap[row.Key] = row.Value.ToMap();
            }

            userProfileMap[UserProfileService.EXPERIMENT_BUCKET_MAP_KEY] = decisionsMap;

            return userProfileMap;
        }
    }
}
