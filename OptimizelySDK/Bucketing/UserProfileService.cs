﻿/* 
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

namespace OptimizelySDK.Bucketing
{

    /// <summary>
    /// Class encapsulating user profile service functionality.
    /// Override with your own implementation for storing and retrieving the user profile.
    /// </summary>
    public abstract class UserProfileService
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

		public abstract Dictionary<string, object> Lookup(String userId);

        public abstract void Save(Dictionary<string, object> userProfile);
    }
}
