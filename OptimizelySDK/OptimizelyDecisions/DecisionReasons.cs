/* 
 * Copyright 2021, Optimizely
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

using System;
using System.Collections.Generic;

namespace OptimizelySDK.OptimizelyDecisions
{
    public class DecisionReasons
    {
        protected List<string> Errors = new List<string>();
        private List<string> Infos = new List<string>();

        public void AddError(string format, params object[] args)
        {
            string message = string.Format(format, args);
            Errors.Add(message);
        }

        public string AddInfo(string format, params object[] args)
        {
            string message = string.Format(format, args);
            Infos.Add(message);

            return message;
        }

        public static DecisionReasons operator +(DecisionReasons a, DecisionReasons b)
        {
            if (b == null) return a;

            a.Errors.AddRange(b.Errors);
            a.Infos.AddRange(b.Infos);

            return a;
        }

        public List<string> ToReport(bool includeReasons = false)
        {
            List<string> reasons = new List<string>(Errors);

            if (includeReasons) {
                reasons.AddRange(Infos);
            }
            
            return reasons;
        }
    }
}
