/* 
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
using System.Linq;

namespace OptimizelySDK.OptimizelyDecisions
{
    public class DefaultDecisionReasons : IDecisionReasons
    {
        private List<string> Errors = new List<string>();
        private List<string> Logs = new List<string>();

        public static IDecisionReasons NewInstance(List<OptimizelyDecideOption> options)
        {
            if (options != null && options.Contains(OptimizelyDecideOption.INCLUDE_REASONS))
            {
                return new DefaultDecisionReasons();
            }
            else
            {
                return new ErrorsDecisionReasons();
            }
        }

        public static IDecisionReasons NewInstance()
        {
            return NewInstance(null);
        }

        public void AddError(string format, params object[] args)
        {
            string message = string.Format(format, args);
            Errors.Add(message);
        }

        public string AddInfo(string format, params object[] args)
        {
            string message = string.Format(format, args);
            Logs.Add(message);
            return message;
        }

        public List<string> ToReport()
        {
            List<string> reasons = new List<string>(Errors);
            reasons.Concat(Logs);
            return reasons;
        }
    }
}
