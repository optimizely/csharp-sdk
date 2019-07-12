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
                                                            UserAttributes userAttributes)
        {
            Variation variation = projectConfig.GetVariationFromId(activatedExperiment?.Key, variationId);
            return CreateImpressionEvent(projectConfig, activatedExperiment, variation, userId, userAttributes);
        }

        /// <summary>
        /// Create ImpressionEvent instance from ProjectConfig
        /// </summary>
        /// <param name="projectConfig">The ProjectConfig entity</param>
        /// <param name="activatedExperiment">The Experiment entity</param>
        /// <param name="variation">The variation entity</param>
        /// <param name="userId">The user Id</param>
        /// <param name="userAttributes">The user's attributes</param>
        /// <returns>ImpressionEvent instance</returns>
        public static ImpressionEvent CreateImpressionEvent(ProjectConfig projectConfig,
                                                            Experiment activatedExperiment,
                                                            Variation variation,
                                                            string userId,
                                                            UserAttributes userAttributes)
        {

            var eventContext = new EventContext.Builder()
                .WithProjectId(projectConfig.ProjectId)
                .WithAccountId(projectConfig.AccountId)
                .WithAnonymizeIP(projectConfig.AnonymizeIP)
                .WithRevision(projectConfig.Revision)                
                .Build();

            return new ImpressionEvent.Builder()
                .WithEventContext(eventContext)
                .WithBotFilteringEnabled(projectConfig.BotFiltering)
                .WithExperiment(activatedExperiment)
                .WithUserId(userId)
                .WithVariation(variation)
                .WithVisitorAttributes(EventFactory.BuildAttributeList(userAttributes, projectConfig))
                .Build();
            
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
                                                            EventTags eventTags)
        {
            

            var eventContext = new EventContext.Builder()
                    .WithProjectId(projectConfig.ProjectId)
                    .WithAccountId(projectConfig.AccountId)
                    .WithAnonymizeIP(projectConfig.AnonymizeIP)
                    .WithRevision(projectConfig.Revision)
                    .Build();

            return new ConversionEvent.Builder()
                .WithBotFilteringEnabled(projectConfig.BotFiltering)
                .WithEventContext(eventContext)
                .WithEventTags(eventTags)
                .WithEvent(projectConfig.GetEvent(eventKey))
                .WithUserId(userId)
                .WithVisitorAttributes(EventFactory.BuildAttributeList(userAttributes, projectConfig))
                .Build();            
        }
    }
}
