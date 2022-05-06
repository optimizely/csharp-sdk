/* 
 * Copyright 2017-2021, Optimizely
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

using OptimizelySDK.Config.audience;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;
using System;
using System.Collections.Generic;

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

        public static Result<bool?> EvaluateAudience(ProjectConfig projectConfig,
                                                                    Experiment experiment,
                                                                    UserAttributes attributes,
                                                                    string loggingEntityType,
                                                                    string loggingKey)
        {
            var reasons = new DecisionReasons();

            var experimentAudienceIds = experiment.AudienceIds;

            // if there are no audiences, ALL users should be part of the experiment
            if (experimentAudienceIds.Length == 0)
            {
                return Result<bool?>.NewResult(true, reasons);
            }

            List<Condition<object>> conditions = new List<Condition<object>>();
            foreach (string audienceId in experimentAudienceIds)
            {
                var condition = new AudienceIdCondition<object>(audienceId);
                conditions.Add(condition);
            }

            var implicitOr = new OrCondition<object>(conditions.ToArray());

            //logger.debug("Evaluating audiences for {} \"{}\": {}.", loggingEntityType, loggingKey, conditions);

            var result = implicitOr.Evaluate(projectConfig, attributes);
            var message = reasons.AddInfo("Audiences for %s \"%s\" collectively evaluated to %s.", loggingEntityType, loggingKey, result);
            //logger.info(message);

            return Result<bool?>.NewResult(result, reasons);
        }

        public static Result<bool?> EvaluateAudienceConditions(ProjectConfig projectConfig,
                                                                Experiment experiment,
                                                                UserAttributes attributes,
                                                                string loggingEntityType,
                                                                string loggingKey)
        {
            var reasons = new DecisionReasons();

            var conditions = experiment.AudienceConditionsList;
            if (conditions == null) return Result<bool?>.NewResult(null, reasons);

            bool? result = null;
            try
            {
                result = conditions.Evaluate(projectConfig, attributes);
                string message = reasons.AddInfo("Audiences for %s \"%s\" collectively evaluated to %s.", loggingEntityType, loggingKey, result);
                //logger.info(message);
            }
            catch (Exception e)
            {
                string message = reasons.AddInfo("Condition invalid: %s", e.Message);
                //logger.error(message);
            }

            return Result<bool?>.NewResult(result, reasons);
        }


        /// <summary>
        /// Check if the user meets audience conditions to be in experiment or not
        /// </summary>
        /// <param name="config">ProjectConfig Configuration for the project</param>
        /// <param name="experiment">Experiment Entity representing the experiment</param>
        /// <param name="userAttributes">Attributes of the user. Defaults to empty attributes array if not provided</param>
        /// <param name="loggingKeyType">It can be either experiment or rule.</param>
        /// <param name="loggingKey">In case loggingKeyType is experiment it will be experiment key or else it will be rule number.</param>
        /// <returns>true if the user meets audience conditions to be in experiment, false otherwise.</returns>
        public static Result<bool> DoesUserMeetAudienceConditions(ProjectConfig config,
            Experiment experiment,
            UserAttributes userAttributes,
            string loggingKeyType,
            string loggingKey,
            ILogger logger)
        {
            var reasons = new DecisionReasons();
            if (userAttributes == null)
                userAttributes = new UserAttributes();

            Result<bool?> decisionResponse;
            if (experiment.AudienceConditions != null)
            {
                //logger.debug("Evaluating audiences for {} \"{}\": {}.", loggingEntityType, loggingKey, experiment.getAudienceConditions());
                decisionResponse = EvaluateAudienceConditions(config, experiment, userAttributes, loggingKeyType, loggingKey);
            }
            else
            {
                decisionResponse = EvaluateAudience(config, experiment, userAttributes, loggingKeyType, loggingKey);
            }
            bool? resolveReturn = decisionResponse.ResultObject;
            reasons += decisionResponse.DecisionReasons;

            return Result<bool>.NewResult(
                resolveReturn != null ? (bool)resolveReturn : false,    // make it Nonnull for if-evaluation
                reasons);
        }
    }
}
