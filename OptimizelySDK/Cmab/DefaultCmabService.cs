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
using OptimizelySDK.Odp;
using OptimizelySDK.OptimizelyDecisions;
using AttributeEntity = OptimizelySDK.Entity.Attribute;

namespace OptimizelySDK.Cmab
{
    /// <summary>
    /// Represents a CMAB decision response returned by the service.
    /// </summary>
    public class CmabDecision
    {
        public CmabDecision(string variationId, string cmabUuid)
        {
            VariationId = variationId;
            CmabUuid = cmabUuid;
        }

        public string VariationId { get; }
        public string CmabUuid { get; }
    }

    public class CmabCacheEntry
    {
        public string AttributesHash { get; set; }
        public string VariationId { get; set; }
        public string CmabUuid { get; set; }
    }

    /// <summary>
    /// Default implementation of the CMAB decision service that handles caching and filtering.
    /// </summary>
    public class DefaultCmabService : ICmabService
    {
        private readonly LruCache<CmabCacheEntry> _cmabCache;
        private readonly ICmabClient _cmabClient;
        private readonly ILogger _logger;

        public DefaultCmabService(LruCache<CmabCacheEntry> cmabCache,
            ICmabClient cmabClient,
            ILogger logger = null)
        {
            _cmabCache = cmabCache;
            _cmabClient = cmabClient;
            _logger = logger ?? new NoOpLogger();
        }

        public CmabDecision GetDecision(ProjectConfig projectConfig,
            OptimizelyUserContext userContext,
            string ruleId,
            OptimizelyDecideOption[] options)
        {
            var optionSet = options ?? new OptimizelyDecideOption[0];
            var filteredAttributes = FilterAttributes(projectConfig, userContext, ruleId);

            if (optionSet.Contains(OptimizelyDecideOption.IGNORE_CMAB_CACHE))
            {
                return FetchDecision(ruleId, userContext.GetUserId(), filteredAttributes);
            }

            if (optionSet.Contains(OptimizelyDecideOption.RESET_CMAB_CACHE))
            {
                _cmabCache.Reset();
            }

            var cacheKey = GetCacheKey(userContext.GetUserId(), ruleId);

            if (optionSet.Contains(OptimizelyDecideOption.INVALIDATE_USER_CMAB_CACHE))
            {
                _cmabCache.Remove(cacheKey);
            }

            var cachedValue = _cmabCache.Lookup(cacheKey);
            var attributesHash = HashAttributes(filteredAttributes);

            if (cachedValue != null)
            {
                if (string.Equals(cachedValue.AttributesHash, attributesHash, StringComparison.Ordinal))
                {
                    return new CmabDecision(cachedValue.VariationId, cachedValue.CmabUuid);
                }

                _cmabCache.Remove(cacheKey);
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

        private CmabDecision FetchDecision(string ruleId,
            string userId,
            UserAttributes attributes)
        {
            var cmabUuid = Guid.NewGuid().ToString();
            var variationId = _cmabClient.FetchDecision(ruleId, userId, attributes, cmabUuid);
            return new CmabDecision(variationId, cmabUuid);
        }

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
                if (attributeIdMap.TryGetValue(attributeId, out AttributeEntity attribute) &&
                    attribute != null &&
                    !string.IsNullOrEmpty(attribute.Key) &&
                    userAttributes.TryGetValue(attribute.Key, out var value))
                {
                    filtered[attribute.Key] = value;
                }
            }

            return filtered;
        }

        private string GetCacheKey(string userId, string ruleId)
        {
            var normalizedUserId = userId ?? string.Empty;
            return $"{normalizedUserId.Length}-{normalizedUserId}-{ruleId}";
        }

        private string HashAttributes(UserAttributes attributes)
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
