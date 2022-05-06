using OptimizelySDK.Entity;
using System;

namespace OptimizelySDK.Config.audience
{
    public class AudienceIdCondition<T> : Condition
    {
        public Audience _Audience { get; set; }
        private readonly string AudienceId;

        public AudienceIdCondition(string audienceId)
        {
            AudienceId = audienceId;
        }

        public bool? Evaluate(ProjectConfig config, UserAttributes attributes)
        {
            if (config != null)
            {
                _Audience = config.AudienceIdMap[AudienceId];
            }
            if (_Audience == null)
            {
                //logger.error("Audience {} could not be found.", audienceId);
                return null;
            }
            //logger.debug("Starting to evaluate audience \"{}\" with conditions: {}.", audience.getId(), audience.getConditions());
            bool? result = _Audience.ParsedConditions.Evaluate(config, attributes);
            //logger.debug("Audience \"{}\" evaluated to {}.", audience.getId(), result);
            return result;
        }

        public string GetOperandOrId()
        {
            return AudienceId;
        }

        public string ToJson()
        {
            throw new NotImplementedException();
        }
    }
}
