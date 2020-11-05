﻿/* 
 * Copyright 2020, Optimizely
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

using System.Collections.Generic;

namespace OptimizelySDK.OptimizelyDecisions
{
    public class ErrorsDecisionReasons : IDecisionReasons
    {
        private readonly List<string> errors = new List<string>();

        public void AddError(string format, params object[] args)
        {
            string message = string.Format(format, args);
            errors.Add(message);
        }

        public string AddInfo(string format, params object[] args)
        {
            // skip tracking and pass-through reasons other than critical errors.
            return string.Format(format, args);
        }

        public List<string> ToReport()
        {
            return errors;
        }
    }
}