/*
 * Copyright 2024 Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Bucketing
{
    public class UserProfileCache
    {
        private readonly Dictionary<string, UserProfile> _cache =
            new Dictionary<string, UserProfile>();

        private readonly UserProfileService _userProfileService;
        private readonly ILogger _logger;

        public UserProfileCache(UserProfileService userProfileService, ILogger logger)
        {
            _userProfileService = userProfileService;
            _logger = logger;
        }

        public UserProfile GetUserProfile(string userId)
        {
            if (_cache.TryGetValue(userId, out var userProfile))
            {
                return _cache[userId];
            }

            var userProfileMap = _userProfileService.Lookup(userId);
            if (userProfileMap != null && UserProfileUtil.IsValidUserProfileMap(userProfileMap))
            {
                userProfile = UserProfileUtil.ConvertMapToUserProfile(userProfileMap);
            }
            else if (userProfileMap == null)
            {
                _logger.Log(LogLevel.INFO,
                    "We were unable to get a user profile map from the UserProfileService.");
            }
            else
            {
                _logger.Log(LogLevel.ERROR, "The UserProfileService returned an invalid map.");
            }

            _cache[userId] = userProfile ??
                             new UserProfile(userId, new Dictionary<string, Decision>());

            return _cache[userId];
        }
    }
}
