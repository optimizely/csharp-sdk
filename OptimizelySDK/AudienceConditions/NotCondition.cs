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

using OptimizelySDK.Entity;
using OptimizelySDK.Logger;

namespace OptimizelySDK.AudienceConditions
{
    /// <summary>
    /// Represents a 'NOT' condition operation for audience evaluation.
    /// </summary>
    public class NotCondition : ICondition
    {
        public ICondition Condition { get; set; }

        public bool? Evaluate(ProjectConfig config, OptimizelyUserContext userContext,
            ILogger logger
        )
        {
            var result = Condition?.Evaluate(config, userContext, logger);
            return result == null ? null : !result;
        }
    }
}
