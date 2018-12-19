using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using System.Linq;


namespace OptimizelySDK.Utils
{
    public class ExperimentUtils
    {
        public static bool IsExperimentActive(Experiment experiment, ILogger logger)
        {

            if (!experiment.IsExperimentRunning)
            {
                logger.Log(LogLevel.INFO, string.Format("Experiment \"{0}\" is not running.", experiment.Key));

                return false;
            }

            return true;
        }


        /// <summary>
        /// Representing whether user meets audience conditions to be in experiment or not
        /// </summary>
        /// <param name="config">ProjectConfig Configuration for the project</param>
        /// <param name="experiment">Experiment Entity representing the experiment</param>
        /// <param name="userAttributes">array Attributes of the user</returns>
        public static bool IsUserInExperiment(ProjectConfig config, Experiment experiment, UserAttributes userAttributes)
        {
            var audienceIds = experiment.AudienceIds;

            if (!audienceIds.Any())
                return true;

            if (userAttributes == null || !userAttributes.Any())
                return false;
            
            var conditionEvaluator = new ConditionEvaluator();
            return audienceIds.Any(id => conditionEvaluator.Evaluate(config.GetAudience(id).ConditionList, userAttributes).GetValueOrDefault());
        }
    }
}
