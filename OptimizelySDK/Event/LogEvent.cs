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
using System.Linq;

namespace OptimizelySDK.Event
{
    public class LogEvent
    {
        /// <summary>
        /// string URL to dispatch log event to
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        /// Parameters to be set in the log event
        /// </summary>
        public Dictionary<string, object> Params { get; private set; }

        /// <summary>
        /// HTTP verb to be used when dispatching the log event
        /// </summary>
        public string HttpVerb { get; private set; }

        /// <summary>
        /// Headers to be set when sending the request
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        public string GetParamsAsJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(Params);
        }

        /// <summary>
        /// LogEvent Construtor
        /// </summary>
        public LogEvent(string url, Dictionary<string, object> parameters, string httpVerb,
            Dictionary<string, string> headers
        )
        {
            Url = url;
            Params = parameters;
            HttpVerb = httpVerb;
            Headers = headers;
        }
    }
}
