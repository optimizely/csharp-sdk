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

namespace OptimizelySDK
{
    public class UserProfile
    {
        /// <summary>
        /// A user's ID.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Map ExperimentId to Decision for the User.
        /// </summary>
        public readonly Dictionary<string, Decision> ExperimentBucketMap;

        /// <summary>
        /// Construct a User Profile instance from explicit components.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="experimentBucketMap">The bucketing experimentBucketMap of the user.</param>
        public UserProfile(string userId, Dictionary<string, Decision> experimentBucketMap)
        {
            UserId = userId;
            ExperimentBucketMap = experimentBucketMap;
        }

        public int HashCode()
        {
            return 31 * UserId.GetHashCode() + ExperimentBucketMap.GetHashCode();
        }

        /// <summary>
        /// Convert a User Profile instance to a Map.
        /// </summary>
        /// <returns>A map representation of the user profile instance.</returns>
        public Dictionary<string, object> ToMap()
        {
            return new Dictionary<string, object>
            {
                { UserProfileService.USER_ID_KEY, UserId },
                { UserProfileService.EXPERIMENT_BUCKET_MAP_KEY,
                    ExperimentBucketMap.ToDictionary(row => row.Key, row => row.Value.ToMap()) }
            };
        }
    }
}
