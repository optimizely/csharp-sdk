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
    public class OptimizelyJSON
    {
        private ILogger Logger;
        private IErrorHandler ErrorHandler;

        private string Payload { get; set; }
        private Dictionary<string, object> Dict { get; set; }

        public OptimizelyJSON(string payload, IErrorHandler errorHandler, ILogger logger)
        {
            try
            {
                ErrorHandler = errorHandler;
                Logger = logger;
                Dict = (Dictionary<string, object>)ConvertIntoCollection(JObject.Parse(payload));
                Payload = payload;
            }
            catch (Exception exception)
            {
                logger.Log(LogLevel.ERROR, "Provided string could not be converted to map.");
                ErrorHandler.HandleError(new Exceptions.InvalidJsonException(exception.Message));
            }
        }

        public OptimizelyJSON(Dictionary<string, object> dict, IErrorHandler errorHandler, ILogger logger)
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
                ErrorHandler.HandleError(new Exceptions.InvalidJsonException(exception.Message));
            }
        }

        override public string ToString()
        {
            return Payload;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return Dict;
        }

        /// <summary>
        /// Returns the value from dictionary of given jsonPath (Seperated by ".") in the provided type T. 
        /// 
        /// Example:
        /// If JSON Data is {"k1":true, "k2":{"k3":"v3"}}
        ///
        /// Set jsonPath to "k2" to access {"k3":"v3"} or set it to "k2.k3" to access "v3"
        /// Set it to null or empty to access the entire JSON data but type must be Dictionary<string, object> as generic type.
        /// </summary>
        /// <param name="jsonPath">Key path for the value.</param>
        /// <returns>Value if decoded successfully</returns>
        public T GetValue<T>(string jsonPath)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonPath))
                {
                    return GetObject<T>(Dict);
                }
                var path = jsonPath.Split('.');

                var currentObject = Dict;
                for (int i = 0; i < path.Length - 1; i++)
                {
                    currentObject = currentObject[path[i]] as Dictionary<string, object>;
                }
                return GetObject<T>(currentObject[path[path.Length - 1]]);
            }
            catch (KeyNotFoundException exception)
            {
                Logger.Log(LogLevel.ERROR, "Value for JSON key not found.");
                ErrorHandler.HandleError(new Exceptions.OptimizelyRuntimeException(exception.Message));
            }
            catch (Exception exception)
            {
                Logger.Log(LogLevel.ERROR, "Value for path could not be assigned to provided type.");
                ErrorHandler.HandleError(new Exceptions.OptimizelyRuntimeException(exception.Message));
            }
            return default(T);
        }

        private T GetObject<T>(object o)
        {
            if (!(o is T deserializedObj))
            {
                deserializedObj = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(o));
            }
            return deserializedObj;
        }

        /// <summary>
        /// This will convert all the given JObjects datatype variables into Dictionaries and JArray objects into List.
        /// </summary>
        /// <param name="o">object containing JObject and JArray datatype objects</param>
        /// <returns>Dictionary object</returns>
        private object ConvertIntoCollection(object o)
        {
            if (o is JObject jo)
            {
                return jo.ToObject<IDictionary<string, object>>().ToDictionary(k => k.Key, v => ConvertIntoCollection(v.Value));
            }
            else if (o is JArray ja)
            {
                return ja.ToObject<List<object>>().Select(ConvertIntoCollection).ToList();
            }
            return o;
        }
    }
}
