/* 
 * Copyright 2017-2020, Optimizely
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

using OptimizelySDK.AudienceConditions;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Utils
{
    public class ExperimentUtils
    {
        public static bool IsExperimentActive(Experiment experiment, ILogger logger)
        {

            if (!experiment.IsExperimentRunning)
            {
                logger.Log(LogLevel.INFO, $"Experiment \"{experiment.Key}\" is not running.");

                return false;
            }

            return true;
        }


        /// <summary>
        /// Check if the user meets audience conditions to be in experiment or not
        /// </summary>
        /// <param name="config">ProjectConfig Configuration for the project</param>
        /// <param name="experiment">Experiment Entity representing the experiment</param>
        /// <param name="userAttributes">Attributes of the user. Defaults to empty attributes array if not provided</param>
        /// <returns>true if the user meets audience conditions to be in experiment, false otherwise.</returns>
        public static bool IsUserInExperiment(ProjectConfig config,
            Experiment experiment,
            UserAttributes userAttributes,
            string audienceFor,
            string loggingKey,
            ILogger logger)
        {
            if (userAttributes == null)
                userAttributes = new UserAttributes();

            ICondition expConditions = null;
            if (experiment.AudienceConditionsList != null)
            {
                expConditions = experiment.AudienceConditionsList;
                logger.Log(LogLevel.DEBUG, $@"Evaluating audiences for {audienceFor} ""{loggingKey}"": {experiment.AudienceConditionsString}.");
            }
            else
            {
                expConditions = experiment.AudienceIdsList;
                logger.Log(LogLevel.DEBUG, $@"Evaluating audiences for {audienceFor} ""{loggingKey}"": {experiment.AudienceIdsString}.");
            }

            // If there are no audiences, return true because that means ALL users are included in the experiment.
            if (expConditions == null)
                return true;

            var result = expConditions.Evaluate(config, userAttributes, logger).GetValueOrDefault();
            var resultText = result.ToString().ToUpper();
            logger.Log(LogLevel.INFO, $@"Audiences for {audienceFor} ""{loggingKey}"" collectively evaluated to {resultText}");
            return result;
        }
    }
}
