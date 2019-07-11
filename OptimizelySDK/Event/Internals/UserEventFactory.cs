using OptimizelySDK.Entity;
using OptimizelySDK.Event.Entity;

namespace OptimizelySDK.Event.internals
{
    public class UserEventFactory
    {
        public static ImpressionEvent CreateImpressionEvent(ProjectConfig projectConfig,
                                                            Experiment activatedExperiment,
                                                            string variationId,
                                                            string userId,
                                                            UserAttributes userAttributes)
        {
            Variation variation = projectConfig.GetVariationFromId(activatedExperiment?.Key, variationId);
            return CreateImpressionEvent(projectConfig, activatedExperiment, variation, userId, userAttributes);
        }

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
