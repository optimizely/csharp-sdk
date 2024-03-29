﻿/* 
 * Copyright 2019-2022, Optimizely
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

using OptimizelySDK.Logger;

namespace OptimizelySDK.AudienceConditions
{
    /// <summary>
    /// Represents Audience Id condition for audience evaluation.
    /// </summary>
    public class AudienceIdCondition : ICondition
    {
        public string AudienceId { get; set; }

        public bool? Evaluate(ProjectConfig config, OptimizelyUserContext userContext,
            ILogger logger
        )
        {
            var audience = config?.GetAudience(AudienceId);
            if (audience == null || string.IsNullOrEmpty(audience.Id))
            {
                return null;
            }

            logger.Log(LogLevel.DEBUG,
                $@"Starting to evaluate audience ""{AudienceId}"" with conditions: {
                    audience.ConditionsString}");
            var result = audience.ConditionList.Evaluate(config, userContext, logger);
            var resultText = result?.ToString().ToUpper() ?? "UNKNOWN";
            logger.Log(LogLevel.DEBUG, $@"Audience ""{AudienceId}"" evaluated to {resultText}");

            return result;
        }
    }
}
