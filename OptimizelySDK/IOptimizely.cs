/*
 * Copyright 2018, 2022-2023 Optimizely, Inc. and contributors
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

#if !(NET35 || NET40 || NETSTANDARD1_6)
#define USE_ODP
#endif

using System.Collections.Generic;
using OptimizelySDK.Entity;
using OptimizelySDK.OptlyConfig;

namespace OptimizelySDK
{
    public interface IOptimizely
    {
        /// <summary>
        /// Returns true if the IOptimizely instance was initialized with a valid datafile
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Buckets visitor and sends impression event to Optimizely.
        /// </summary>
        /// <param name="experimentKey">experimentKey string Key identifying the experiment</param>
        /// <param name="userId">string ID for user</param>
        /// <param name="userAttributes">associative array of Attributes for the user</param>
        /// <returns>null|Variation Representing variation</returns>
        Variation Activate(string experimentKey, string userId, UserAttributes userAttributes = null
        );

        /// <summary>
        /// Create a context of the user for which decision APIs will be called.
        /// A user context will be created successfully even when the SDK is not fully configured yet.
        /// </summary>
        /// <param name="userId">The user ID to be used for bucketing.</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>OptimizelyUserContext | An OptimizelyUserContext associated with this OptimizelyClient.</returns>
        OptimizelyUserContext
            CreateUserContext(string userId, UserAttributes userAttributes = null);

        /// <summary>
        /// Sends conversion event to Optimizely.
        /// </summary>
        /// <param name="eventKey">Event key representing the event which needs to be recorded</param>
        /// <param name="userId">ID for user</param>
        /// <param name="userAttributes">Attributes of the user</param>
        /// <param name="eventTags">eventTags array Hash representing metadata associated with the event.</param>
        void Track(string eventKey, string userId, UserAttributes userAttributes = null,
            EventTags eventTags = null
        );

        /// <summary>
        /// Get variation where user will be bucketed
        /// </summary>
        /// <param name="experimentKey">experimentKey string Key identifying the experiment</param>
        /// <param name="userId">ID for the user</param>
        /// <param name="userAttributes">Attributes for the users</param>
        /// <returns>null|Variation Representing variation</returns>
        Variation GetVariation(string experimentKey, string userId,
            UserAttributes userAttributes = null
        );

        /// <summary>
        /// Force a user into a variation for a given experiment.
        /// </summary>
        /// <param name="experimentKey">The experiment key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="variationKey">The variation key specifies the variation which the user will be forced into.
        /// If null, then clear the existing experiment-to-variation mapping.</param>
        /// <returns>A boolean value that indicates if the set completed successfully.</returns>
        bool SetForcedVariation(string experimentKey, string userId, string variationKey);

        /// <summary>
        /// Gets the forced variation key for the given user and experiment.
        /// </summary>
        /// <param name="experimentKey">The experiment key</param>
        /// <param name="userId">The user ID</param>
        /// <returns>null|string The variation key.</returns>
        Variation GetForcedVariation(string experimentKey, string userId);

        #region FeatureFlag APIs

        /// <summary>
        /// Determine whether a feature is enabled.
        /// Send an impression event if the user is bucketed into an experiment using the feature.
        /// </summary>
        /// <param name="featureKey">The feature key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes.</param>
        /// <returns>True if feature is enabled, false or null otherwise</returns>
        bool IsFeatureEnabled(string featureKey, string userId, UserAttributes userAttributes = null
        );

        /// <summary>
        /// Gets boolean feature variable value.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="variableKey">The variable key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>bool | Feature variable value or null</returns>
        bool? GetFeatureVariableBoolean(string featureKey, string variableKey, string userId,
            UserAttributes userAttributes = null
        );

        /// <summary>
        /// Gets double feature variable value.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="variableKey">The variable key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>double | Feature variable value or null</returns>
        double? GetFeatureVariableDouble(string featureKey, string variableKey, string userId,
            UserAttributes userAttributes = null
        );

        /// <summary>
        /// Gets integer feature variable value.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="variableKey">The variable key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>int | Feature variable value or null</returns>
        int? GetFeatureVariableInteger(string featureKey, string variableKey, string userId,
            UserAttributes userAttributes = null
        );

        /// <summary>
        /// Gets string feature variable value.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="variableKey">The variable key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>string | Feature variable value or null</returns>
        string GetFeatureVariableString(string featureKey, string variableKey, string userId,
            UserAttributes userAttributes = null
        );

        /// <summary>
        /// Gets json sub type feature variable value.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="variableKey">The variable key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>OptimizelyJson | Feature variable value or null</returns>
        OptimizelyJSON GetFeatureVariableJSON(string featureKey, string variableKey,
            string userId, UserAttributes userAttributes = null
        );

        /// <summary>
        /// Get the values of all variables in the feature.
        /// </summary>
        /// <param name="featureKey">The feature flag key</param>
        /// <param name="userId">The user ID</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>string | null An OptimizelyJSON instance for all variable values.</returns>
        OptimizelyJSON GetAllFeatureVariables(string featureKey, string userId,
            UserAttributes userAttributes = null
        );

        /// <summary>
        /// Get the list of features that are enabled for the user.
        /// </summary>
        /// <param name="userId">The user Id</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>List of the feature keys that are enabled for the user.</returns>
        List<string> GetEnabledFeatures(string userId, UserAttributes userAttributes = null);

        /// <summary>
        /// Get OptimizelyConfig containing experiments and features map
        /// </summary>
        /// <returns>OptimizelyConfig Object</returns>
        OptimizelyConfig GetOptimizelyConfig();
        #endregion

#if USE_ODP
        void SendOdpEvent(string action, Dictionary<string, string> identifiers, string type,
            Dictionary<string, object> data
        );
#endif
    }
}
