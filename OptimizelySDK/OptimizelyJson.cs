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

using OptimizelySDK.Logger;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OptimizelySDK.ErrorHandler;
using System.Linq;
using Newtonsoft.Json;

namespace OptimizelySDK
{
    public class OptimizelyJson
    {
        private ILogger Logger;
        private IErrorHandler ErrorHandler;

        private string Payload { get; set; }
        private Dictionary<string, object> Dict { get; set; }

        public OptimizelyJson(string payload, IErrorHandler errorHandler, ILogger logger)
        {
            try
            {
                ErrorHandler = errorHandler;
                Logger = logger;
                Dict = (Dictionary<string, object>)ToCollections(JObject.Parse(payload));
                Payload = payload;
            }
            catch (Exception exception)
            {
                logger.Log(LogLevel.ERROR, "Provided string could not be converted to map.");
                ErrorHandler.HandleError(new Exceptions.OptimizelyRuntimeException(exception.Message));
            }
        }
        public OptimizelyJson(Dictionary<string, object> dict, IErrorHandler errorHandler, ILogger logger)
        {
            try
            {
                ErrorHandler = errorHandler;
                Logger = logger;
                Payload = JsonConvert.SerializeObject(dict);
                Dict = dict;
            }
            catch (Exception exception)
            {
                logger.Log(LogLevel.ERROR, "Provided map could not be converted to string.");
                ErrorHandler.HandleError(new Exceptions.OptimizelyRuntimeException(exception.Message));
            }
        }
        public static object ToCollections(object o)
        {
            if (o is JObject jo) return jo.ToObject<IDictionary<string, object>>().ToDictionary(k => k.Key, v => ToCollections(v.Value));
            if (o is JArray ja) return ja.ToObject<List<object>>().Select(ToCollections).ToList();
            return o;
        }

        override
        public string ToString()
        { 
            return Payload;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return Dict;
        }

        /// <summary>
        /// If JSON Data is {"k1":true, "k2":{"k3":"v3"}}
        ///
        /// Set jsonPath to "k2" to access {"k3":"v3"} or set it to "k2.k3" to access "v3"
        /// Set it to nil or empty to access the entire JSON data.
        /// </summary>
        /// <param name="jsonPath">Key path for the value.</param>
        /// <returns>Value if decoded successfully</returns>
        public T GetValue<T>(string jsonPath)
        {
            try
            {
                string[] path = jsonPath.Split('.');
                Dictionary<string, object> currentObject = Dict;
                for (int i = 0; i < path.Length - 1; i++)
                {
                   currentObject = currentObject[path[i]] as Dictionary<string, object>;
                }
                return (T)currentObject[path[path.Length - 1]];
            }
            catch (Exception exception)
            {
                Logger.Log(LogLevel.ERROR, "Value for path could not be assigned to provided type.");
                ErrorHandler.HandleError(new Exceptions.InvalidCastException(exception.Message));
            }
            return default(T);
        }
    }
}
