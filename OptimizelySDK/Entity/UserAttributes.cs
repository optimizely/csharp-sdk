/* 
 * Copyright 2017, Optimizely
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
using OptimizelySDK.Logger;

namespace OptimizelySDK.Entity
{
    public class UserAttributes : Dictionary<string, object>
    {
        public UserAttributes FilterNullValues(ILogger logger)
        {
            UserAttributes answer = new UserAttributes();
            foreach (KeyValuePair<string, object> pair in this) {
                if (pair.Value != null) {
                    answer[pair.Key] = pair.Value;
                } else {
                    logger.Log(LogLevel.ERROR, string.Format("[UserAttributes] Null value for key {0} removed and will not be sent to results.", pair.Key));
                }
            }
            return answer;
        }
    }
}
