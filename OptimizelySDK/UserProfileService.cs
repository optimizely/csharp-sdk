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

	/// <summary>
	/// Class encapsulating user profile service functionality.
	/// Override with your own implementation for storing and retrieving the user profile.
	/// </summary>
	
   	abstract public class UserProfileService
    {
        public const string USER_ID_KEY = "user_id";
        public const string DECISIONS_KEY = "decisions";
        public const string VARIATION_ID_KEY = "variation_id";

        /// <summary>
        /// A user's ID.
        /// </summary>
        public readonly string UserId;


		/// <summary>
		/// The bucketing decisions of the user.
		/// </summary> 
        private readonly Dictionary<string, string> Decisions;


		/// <summary>
		/// Construct a User Profile instance from explicit components.
		/// </summary>
		/// <param name="userId">userId The ID of the user.</param>
		/// <param name="decisions">The bucketing decisions of the user.</param>
		public UserProfileService(String userId, Dictionary<string, string> decisions)
        {
            this.UserId = userId;
            this.Decisions = decisions;
        }


		/// <summary>
		/// Construct a User Profile instance from a Map.
		/// </summary>
		/// <param name="userProfileMap">containing the properties of the user profile.</param>
				public UserProfileService(Dictionary<string, object> userProfileMap)
            : this((string)userProfileMap[USER_ID_KEY], (Dictionary<string, string>)userProfileMap[DECISIONS_KEY])
        {

        }

        /**
         * Convert a User Profile instance to a Map.
         * @return A map representation of the user profile instance.
         */
        public Dictionary<string, object> ToMap()
        {
            Dictionary<string, object> userProfileMap = new Dictionary<string, object>();
            userProfileMap[USER_ID_KEY] = UserId;
            userProfileMap[DECISIONS_KEY] = Decisions;

            return userProfileMap;
        }
		
        /// <summary>
		/// Fetch the user profile map for the user ID.
		/// </summary>
		/// <returns>
        ///     a Map representing the user's profile.
        /// {
        ///     userIdKey : String userId,
        ///     decisionsKey : 
        ///     {
	    ///         experimentId : variationId
        ///     }
	    /// }
        /// </returns>
		/// <param name="userId">The ID of the user whose profile will be retrieved.</param>
		public abstract Dictionary<string, object> Lookup(String userId);



        public abstract void Save(Dictionary<string, object> userProfile);
    }
}
