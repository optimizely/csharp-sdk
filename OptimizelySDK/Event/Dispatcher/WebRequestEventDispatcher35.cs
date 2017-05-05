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
#if NET35 || NET40
using System;
using System.IO;
using System.Net;

namespace OptimizelySDK.Event.Dispatcher
{
    public class WebRequestClientEventDispatcher35 : IEventDispatcher
    {
        // TODO Catch and Log Errors
        public Logger.ILogger Logger { get; set; }

        /// <summary>
        /// Timeout for the HTTP request (10 seconds)
        /// </summary>
        private const int TIMEOUT_MS = 10000;

        private HttpWebRequest Request = null;

        /// <summary>
        /// Dispatch the Event
        /// The call will not wait for the result, it returns after sending (fire and forget)
        /// But it does get called back asynchronously when the response comes and handles
        /// </summary>
        /// <param name="logEvent"></param>
        public void DispatchEvent(LogEvent logEvent)
        {
            Request = (HttpWebRequest)WebRequest.Create(logEvent.Url);

            Request.UserAgent = "Optimizely-csharp-SDKv01";
            Request.Timeout = TIMEOUT_MS;
            Request.Method = logEvent.HttpVerb;

            foreach (var h in logEvent.Headers)
                Request.Headers[h.Key] = h.Value;

            Request.ContentType = "application/json";

            using (var streamWriter = new StreamWriter(Request.GetRequestStream()))
            {
                streamWriter.Write(logEvent.GetParamsAsJson());
                streamWriter.Flush();
                streamWriter.Close();
            }

            IAsyncResult result = Request.BeginGetRequestStream(new AsyncCallback(FinaliseHttpAsyncRequest), this);
        }

        private static void FinaliseHttpAsyncRequest(IAsyncResult result)
        {
            var _this = (WebRequestClientEventDispatcher35)result.AsyncState;
            _this.FinalizeRequest(result);
        }

        private void FinalizeRequest(IAsyncResult result)
        {
            var response = (HttpWebResponse)Request.EndGetResponse(result);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                // Read the results, even though we don't need it.
                Stream responseStream = response.GetResponseStream();
                var streamEncoder = System.Text.Encoding.UTF8;
                StreamReader responseReader = new StreamReader(responseStream, streamEncoder);
                string data = responseReader.ReadToEnd();
            }
            else
            {
                // TODO: Add Logger and capture exception
                //throw new Exception(string.Format("Response Not Valid {0}", response.StatusCode));
            }
        }
    }
}
#endif