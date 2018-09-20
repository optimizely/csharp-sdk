﻿/* 
 * Copyright 2017-2018, Optimizely
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
using OptimizelySDK.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using Attribute = OptimizelySDK.Entity.Attribute;

namespace OptimizelySDK.Utils
{
    public static class Validator
    {
        /// <summary>
        /// Validate the ProjectConfig JSON
        /// </summary>
        /// <param name="configJson">ProjectConfig JSON</param>
        /// <param name="schemaJson">Schema JSON for ProjectConfig.  If none is provided, use the one already in the project</param>
        /// <returns>Whether the ProjectConfig is valid</returns>
#if !NET35
        public static bool ValidateJSONSchema(string configJson, string schemaJson = null)
        {
            try
            {
                return !NJsonSchema.JsonSchema4
                    .FromJsonAsync(schemaJson ?? Schema.GetSchemaJson())
                    .Result
                    .Validate(configJson)
                    .Any();
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                return false;
            }
        }
#endif
#if NET35
        public static bool ValidateJSONSchema(string configJson, string schemaJson = null)
        {
            return true;
        }
#endif

        /// <summary>
        /// Determines whether all attributes in an array are valid.
        /// </summary>
        /// <param name="attributes">Mixed attributes of the user</param>
        /// <returns>true iff all attributes are valid.</returns>
        public static bool AreAttributesValid(IEnumerable<Attribute> attributes)
        {
            // This method doesn't have any purpose, we may delete it.
            // In PHP it's for type checking.
            return attributes.All(IsAttributeValid);
        }

        public static bool IsAttributeValid(Attribute attribute)
        {
            int key;
            return !int.TryParse(attribute.Key, out key);
        }

        public static bool AreEventTagsValid(Dictionary<string, object> eventTags) {
            int key;
            return eventTags.All(tag => !int.TryParse(tag.Key, out key));

        }

        /// <summary>
        /// Determines whether all the experiments in the feature flag
        /// belongs to the same mutex group
        /// </summary>
        /// <param name="projectConfig">The project config object</param>
        /// <param name="featureFlag">Feature flag to validate</param>
        /// <returns>true if feature flag is valid.</returns>
        public static bool IsFeatureFlagValid(ProjectConfig projectConfig, FeatureFlag featureFlag)
        {
            var experimentIds = featureFlag.ExperimentIds;

            if (experimentIds == null || experimentIds.Count <= 1)
                return true;

            var groupId = projectConfig.GetExperimentFromId(experimentIds[0]).GroupId;

            for (int i = 1; i < experimentIds.Count; i++)
            {
                // Every experiment should have the same group Id.
                if (projectConfig.GetExperimentFromId(experimentIds[i]).GroupId != groupId)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determine if given user attribute is valid.
        /// </summary>
        /// <param name="attribute">Attribute key and value pair</param>
        /// <returns>true if attribute key is not null and value is one of the supported type, false otherwise</returns>
        public static bool IsUserAttributeValid(KeyValuePair<string, object> attribute)
        {
            return (attribute.Key != null) && 
                (attribute.Value is int || attribute.Value is string || attribute.Value is double 
                 || attribute.Value is bool || attribute.Value is float || attribute.Value is long);
        }
    }
}

