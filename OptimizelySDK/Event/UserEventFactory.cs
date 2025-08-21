/**
 *
 *    Copyright 2019-2020, Optimizely and contributors
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */

using OptimizelySDK.Entity;
using OptimizelySDK.Event.Entity;

namespace OptimizelySDK.Event
{
    /// <summary>
    /// UserEventFactory builds ImpressionEvent and ConversionEvent objects from a given UserEvent.
    /// </summary>
    public class UserEventFactory
    {
        /// <summary>
        /// Create ImpressionEvent instance from ProjectConfig
        /// </summary>
        /// <param name="projectConfig">The ProjectConfig entity</param>
        /// <param name="activatedExperiment">The Experiment entity</param>
        /// <param name="variationId">The variation Id</param>
        /// <param name="userId">The user Id</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>ImpressionEvent instance</returns>
        public static ImpressionEvent CreateImpressionEvent(ProjectConfig projectConfig,
            Experiment activatedExperiment,
            string variationId,
            string userId,
            UserAttributes userAttributes,
            string flagKey,
            string ruleType,
            bool enabled = false
        )
        {
            var variation = projectConfig.GetVariationFromId(activatedExperiment?.Key, variationId);
            return CreateImpressionEvent(projectConfig, activatedExperiment, variation, userId,
                userAttributes, flagKey, ruleType, enabled);
        }

        /// <summary>
        /// Create ImpressionEvent instance from ProjectConfig
        /// </summary>
        /// <param name="projectConfig">The ProjectConfig entity</param>
        /// <param name="activatedExperiment">The Experiment entity</param>
        /// <param name="variation">The variation entity</param>
        /// <param name="userId">The user Id</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <param name="flagKey">experiment key or feature key</param>
        /// <param name="ruleType">experiment or featureDecision source </param>
        /// <returns>ImpressionEvent instance</returns>
        public static ImpressionEvent CreateImpressionEvent(ProjectConfig projectConfig,
            ExperimentCore activatedExperiment,
            Variation variation,
            string userId,
            UserAttributes userAttributes,
            string flagKey,
            string ruleType,
            bool enabled = false
        )
        {
            if ((ruleType == FeatureDecision.DECISION_SOURCE_ROLLOUT || variation == null) &&
                !projectConfig.SendFlagDecisions)
            {
                return null;
            }

            var eventContext = new EventContext.Builder().WithProjectId(projectConfig.ProjectId).
                WithAccountId(projectConfig.AccountId).
                WithAnonymizeIP(projectConfig.AnonymizeIP).
                WithRevision(projectConfig.Revision).
                WithRegion(projectConfig.Region).
                Build();

            var variationKey = "";
            var ruleKey = "";
            if (variation != null)
            {
                variationKey = variation.Key;
                ruleKey = activatedExperiment?.Key ?? string.Empty;
            }

            var metadata = new DecisionMetadata(flagKey, ruleKey, ruleType, variationKey, enabled);

            return new ImpressionEvent.Builder().WithEventContext(eventContext).
                WithBotFilteringEnabled(projectConfig.BotFiltering).
                WithExperiment(activatedExperiment).
                WithMetadata(metadata).
                WithUserId(userId).
                WithVariation(variation).
                WithVisitorAttributes(
                    EventFactory.BuildAttributeList(userAttributes, projectConfig)).
                Build();
        }

        /// <summary>
        /// Create ConversionEvent instance from ProjectConfig
        /// </summary>
        /// <param name="projectConfig">The ProjectConfig entity</param>
        /// <param name="eventKey">The event key</param>
        /// <param name="userId">The user Id</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <param name="eventTags">Array Hash representing metadata associated with the event.</param>
        /// <returns>ConversionEvent instance</returns>
        public static ConversionEvent CreateConversionEvent(ProjectConfig projectConfig,
            string eventKey,
            string userId,
            UserAttributes userAttributes,
            EventTags eventTags
        )
        {
            var eventContext = new EventContext.Builder().WithProjectId(projectConfig.ProjectId).
                WithAccountId(projectConfig.AccountId).
                WithAnonymizeIP(projectConfig.AnonymizeIP).
                WithRevision(projectConfig.Revision).
                WithRegion(projectConfig.Region).
                Build();

            return new ConversionEvent.Builder().
                WithBotFilteringEnabled(projectConfig.BotFiltering).
                WithEventContext(eventContext).
                WithEventTags(eventTags).
                WithEvent(projectConfig.GetEvent(eventKey)).
                WithUserId(userId).
                WithVisitorAttributes(
                    EventFactory.BuildAttributeList(userAttributes, projectConfig)).
                Build();
        }
    }
}
