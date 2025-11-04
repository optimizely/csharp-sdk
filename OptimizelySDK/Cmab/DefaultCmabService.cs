/* 
* Copyright 2025, Optimizely
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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using OptimizelySDK;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;
using OptimizelySDK.Utils;
using AttributeEntity = OptimizelySDK.Entity.Attribute;

namespace OptimizelySDK.Cmab
{
    /// <summary>
    /// Represents a CMAB decision response returned by the service.
    /// </summary>
    public class CmabDecision
    {
        /// <summary>
        /// Initializes a new instance of the CmabDecision class.
        /// </summary>
        /// <param name="variationId">The variation ID assigned by the CMAB service.</param>
        /// <param name="cmabUuid">The unique identifier for this CMAB decision.</param>
        public CmabDecision(string variationId, string cmabUuid)
        {
            VariationId = variationId;
            CmabUuid = cmabUuid;
        }

        /// <summary>
        /// Gets the variation ID assigned by the CMAB service.
        /// </summary>
        public string VariationId { get; }

        /// <summary>
        /// Gets the unique identifier for this CMAB decision.
        /// </summary>
        public string CmabUuid { get; }
    }

    /// <summary>
    /// Represents a cached CMAB decision entry.
    /// </summary>
    public class CmabCacheEntry
    {
        /// <summary>
        /// Gets or sets the hash of the filtered attributes used for this decision.
        /// </summary>
        public string AttributesHash { get; set; }

        /// <summary>
        /// Gets or sets the variation ID from the cached decision.
        /// </summary>
        public string VariationId { get; set; }

        /// <summary>
        /// Gets or sets the CMAB UUID from the cached decision.
        /// </summary>
        public string CmabUuid { get; set; }
    }

    /// <summary>
    /// Default implementation of the CMAB decision service that handles caching and filtering.
    /// Provides methods for retrieving CMAB decisions with intelligent caching based on user attributes.
    /// </summary>
    public class DefaultCmabService : ICmabService
    {
        /// <summary>
        /// Number of lock stripes to use for concurrency control.
        /// Using multiple locks reduces contention while ensuring the same user/rule combination always uses the same lock.
        /// </summary>
        private const int NUM_LOCK_STRIPES = 1000;

        private readonly ICacheWithRemove<CmabCacheEntry> _cmabCache;
        private readonly ICmabClient _cmabClient;
        private readonly ILogger _logger;
        private readonly object[] _locks;

        /// <summary>
        /// Initializes a new instance of the DefaultCmabService class.
        /// </summary>
        /// <param name="cmabCache">Cache for storing CMAB decisions.</param>
        /// <param name="cmabClient">Client for fetching decisions from the CMAB prediction service.</param>
        /// <param name="logger">Logger for recording service operations.</param>
        public DefaultCmabService(ICacheWithRemove<CmabCacheEntry> cmabCache,
            ICmabClient cmabClient,
            ILogger logger)
        {
            _cmabCache = cmabCache;
            _cmabClient = cmabClient;
            _logger = logger;
            _locks = Enumerable.Range(0, NUM_LOCK_STRIPES).Select(_ => new object()).ToArray();
        }

        /// <summary>
        /// Calculate the lock index for a given user and rule combination.
        /// Uses MurmurHash to ensure consistent lock selection for the same user/rule while distributing different combinations across locks.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="ruleId">The experiment/rule ID.</param>
        /// <returns>The lock index in the range [0, NUM_LOCK_STRIPES).</returns>
        internal int GetLockIndex(string userId, string ruleId)
        {
            var hashInput = $"{userId}{ruleId}";
            var murmer32 = Murmur.MurmurHash.Create32(0, true);
            var data = Encoding.UTF8.GetBytes(hashInput);
            var hash = murmer32.ComputeHash(data);
            var hashValue = BitConverter.ToUInt32(hash, 0);
            return (int)(hashValue % NUM_LOCK_STRIPES);
        }

        public CmabDecision GetDecision(ProjectConfig projectConfig,
            OptimizelyUserContext userContext,
            string ruleId,
            OptimizelyDecideOption[] options = null)
        {
            var lockIndex = GetLockIndex(userContext.GetUserId(), ruleId);
            lock (_locks[lockIndex])
            {
                return GetDecisionInternal(projectConfig, userContext, ruleId, options);
            }
        }

        /// <summary>
        /// Internal implementation of GetDecision that performs the actual decision logic.
        /// This method should only be called while holding the appropriate lock.
        /// </summary>
        private CmabDecision GetDecisionInternal(ProjectConfig projectConfig,
            OptimizelyUserContext userContext,
            string ruleId,
            OptimizelyDecideOption[] options = null)
        {
            var optionSet = options ?? new OptimizelyDecideOption[0];
            var filteredAttributes = FilterAttributes(projectConfig, userContext, ruleId);

            if (optionSet.Contains(OptimizelyDecideOption.IGNORE_CMAB_CACHE))
            {
                _logger.Log(LogLevel.DEBUG, "Ignoring CMAB cache.");
                return FetchDecision(ruleId, userContext.GetUserId(), filteredAttributes);
            }

            if (optionSet.Contains(OptimizelyDecideOption.RESET_CMAB_CACHE))
            {
                _logger.Log(LogLevel.DEBUG, "Resetting CMAB cache.");
                _cmabCache.Reset();
            }

            var cacheKey = GetCacheKey(userContext.GetUserId(), ruleId);

            if (optionSet.Contains(OptimizelyDecideOption.INVALIDATE_USER_CMAB_CACHE))
            {
                _logger.Log(LogLevel.DEBUG, "Invalidating user CMAB cache.");
                _cmabCache.Remove(cacheKey);
            }

            var cachedValue = _cmabCache.Lookup(cacheKey);
            var attributesHash = HashAttributes(filteredAttributes);

            if (cachedValue != null)
            {
                if (string.Equals(cachedValue.AttributesHash, attributesHash, StringComparison.Ordinal))
                {
                    _logger.Log(LogLevel.DEBUG, "CMAB cache hit.");
                    return new CmabDecision(cachedValue.VariationId, cachedValue.CmabUuid);
                }
                else
                {
                    _cmabCache.Remove(cacheKey);
                }

            }

            var cmabDecision = FetchDecision(ruleId, userContext.GetUserId(), filteredAttributes);

            _cmabCache.Save(cacheKey, new CmabCacheEntry
            {
                AttributesHash = attributesHash,
                VariationId = cmabDecision.VariationId,
                CmabUuid = cmabDecision.CmabUuid,
            });

            return cmabDecision;
        }

        /// <summary>
        /// Fetches a new decision from the CMAB client and generates a unique UUID for tracking.
        /// </summary>
        /// <param name="ruleId">The experiment/rule ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="attributes">The filtered user attributes to send to the CMAB service.</param>
        /// <returns>A new CmabDecision with the assigned variation and generated UUID.</returns>
        private CmabDecision FetchDecision(string ruleId,
            string userId,
            UserAttributes attributes)
        {
            var cmabUuid = Guid.NewGuid().ToString();
            var variationId = _cmabClient.FetchDecision(ruleId, userId, attributes, cmabUuid);
            return new CmabDecision(variationId, cmabUuid);
        }

        /// <summary>
        /// Filters user attributes to include only those configured for the CMAB experiment.
        /// </summary>
        /// <param name="projectConfig">The project configuration containing attribute mappings.</param>
        /// <param name="userContext">The user context with all user attributes.</param>
        /// <param name="ruleId">The experiment/rule ID to get CMAB attribute configuration for.</param>
        /// <returns>A UserAttributes object containing only the filtered attributes, or empty if no CMAB config exists.</returns>
        /// <remarks>
        /// Only attributes specified in the experiment's CMAB configuration are included.
        /// This ensures that cache invalidation is based only on relevant attributes.
        /// </remarks>
        private UserAttributes FilterAttributes(ProjectConfig projectConfig,
            OptimizelyUserContext userContext,
            string ruleId)
        {
            var filtered = new UserAttributes();

            if (projectConfig.ExperimentIdMap == null ||
                !projectConfig.ExperimentIdMap.TryGetValue(ruleId, out var experiment) ||
                experiment?.Cmab?.AttributeIds == null ||
                experiment.Cmab.AttributeIds.Count == 0)
            {
                return filtered;
            }

            var userAttributes = userContext.GetAttributes() ?? new UserAttributes();
            var attributeIdMap = projectConfig.AttributeIdMap ?? new Dictionary<string, AttributeEntity>();

            foreach (var attributeId in experiment.Cmab.AttributeIds)
            {
                if (attributeIdMap.TryGetValue(attributeId, out var attribute) &&
                    userAttributes.TryGetValue(attribute.Key, out var value))
                {
                    filtered[attribute.Key] = value;
                }
            }

            return filtered;
        }

        /// <summary>
        /// Generates a cache key for storing and retrieving CMAB decisions.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="ruleId">The experiment/rule ID.</param>
        /// <returns>A cache key string in the format: {userId.Length}-{userId}-{ruleId}</returns>
        /// <remarks>
        /// The length prefix prevents key collisions between different user IDs that might appear
        /// similar when concatenated (e.g., "12-abc-exp" vs "1-2abc-exp").
        /// </remarks>
        internal static string GetCacheKey(string userId, string ruleId)
        {
            var normalizedUserId = userId ?? string.Empty;
            return $"{normalizedUserId.Length}-{normalizedUserId}-{ruleId}";
        }

        /// <summary>
        /// Computes an MD5 hash of the user attributes for cache validation.
        /// </summary>
        /// <param name="attributes">The user attributes to hash.</param>
        /// <returns>A hexadecimal MD5 hash string of the serialized attributes.</returns>
        /// <remarks>
        /// Attributes are sorted by key before hashing to ensure consistent hashes regardless of
        /// the order in which attributes are provided. This allows cache hits when the same attributes
        /// are present in different orders.
        /// </remarks>
        internal static string HashAttributes(UserAttributes attributes)
        {
            var ordered = attributes.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var serialized = JsonConvert.SerializeObject(ordered);

            using (var md5 = MD5.Create())
            {
                var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(serialized));
                var builder = new StringBuilder(hashBytes.Length * 2);
                foreach (var b in hashBytes)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
