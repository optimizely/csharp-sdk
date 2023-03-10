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
    public class UserProfile
    {
        /// <summary>
        /// The key for the user ID. Returns a String.
        /// </summary>
        public const string USER_ID_KEY = "user_id";

        /// <summary>
        /// The key for the decisions Map. Returns a Dictionary<String, Map<String, String>>
        /// </summary>
        public const string EXPERIMENT_BUCKET_MAP_KEY = "experiment_bucket_map";

        /** The key for the variation Id within a decision Map. */
        public const string VARIATION_ID_KEY = "variation_id";


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

        /// <summary>
        /// Convert a User Profile instance to a Map.
        /// </summary>
        /// <returns>A map representation of the user profile instance.</returns>
        public Dictionary<string, object> ToMap()
        {
            return new Dictionary<string, object>
            {
                { USER_ID_KEY, UserId },
                {
                    EXPERIMENT_BUCKET_MAP_KEY,
                    ExperimentBucketMap.ToDictionary(row => row.Key, row => row.Value.ToMap())
                },
            };
        }
    }
}
