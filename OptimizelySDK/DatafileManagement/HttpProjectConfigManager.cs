/* 
 * Copyright 2019, Optimizely
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
using System.Linq;
using System.Net.Http;

namespace OptimizelySDK.DatafileManagement
{
    public class HttpProjectConfigManager : PollingProjectConfigManager
    {
        private string Url;
        public HttpClient Client;
        private string LastModifiedSince = string.Empty;
        
        private HttpProjectConfigManager(TimeSpan period, string url, ILogger logger) : base(period, logger)
        {
            Client = new HttpClient();
            Url = url;
        }

        static ProjectConfig ParseProjectConfig(string datafile)
        {
            return DatafileProjectConfig.Create(datafile, null, null);
        }

        protected override ProjectConfig Poll()
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(Url),
                Method = HttpMethod.Get,
            };
            
            // Send If-Modified-Since header if Last-Modified-Since header contains any value.
            if (!string.IsNullOrEmpty(LastModifiedSince))
                request.Headers.Add("If-Modified-Since", LastModifiedSince);

            var httpResponse = Client.SendAsync(request);
            httpResponse.Wait();

            // Return from here if datafile is not modified.
            var response = httpResponse.Result;
            if (!response.IsSuccessStatusCode)
            {
                Logger.Log(LogLevel.ERROR, "Unexpected response from event endpoint, status: " + response.StatusCode);
                return null;
            }

            // Update Last-Modified header if provided.
            if (response.Headers.TryGetValues("Last-Modified", out IEnumerable<string> values))
                LastModifiedSince = values.First();

            var content = response.Content.ReadAsStringAsync();
            content.Wait();
            
            string datafile = content.Result.ToString();
            return ParseProjectConfig(datafile);
        }
        
        public class Builder
        {
            private string Datafile;
            private string SdkKey;
            private string Url;
            private string Format = "https://cdn.optimizely.com/datafiles/{0}.json";
            private ILogger Logger;
            private TimeSpan Period;

            public Builder WithDatafile(string datafile)
            {
                Datafile = datafile;
                return this;
            }

            public Builder WithSdkKey(string sdkKey)
            {
                SdkKey = sdkKey;
                return this;
            }

            public Builder WithUrl(string url)
            {
                Url = url;
                return this;
            }

            public Builder WithPollingInterval(TimeSpan period)
            {
                Period = period;
                return this;
            }

            public Builder WithFormat(string format)
            {
                Format = format;
                return this;
            }
            
            public Builder WithLogger(ILogger logger)
            {
                Logger = logger;
                return this;
            }

            /// <summary>
            /// HttpProjectConfigManager.Builder that builds and starts a HttpProjectConfigManager.
            /// This is the default builder which will block until a config is available.
            /// </summary>
            /// <returns>HttpProjectConfigManager instance</returns>
            public HttpProjectConfigManager Build()
            {
                return Build(false);
            }

            /// <summary>
            /// HttpProjectConfigManager.Builder that builds and starts a HttpProjectConfigManager.
            /// </summary>
            /// <param name="defer">When true, we will not wait for the configuration to be available
            /// before returning the HttpProjectConfigManager instance.</param>
            /// <returns>HttpProjectConfigManager instance</returns>
            public HttpProjectConfigManager Build(bool defer)
            {
                if (Logger == null)
                    Logger = new DefaultLogger();

                if (!string.IsNullOrEmpty(Url))
                    return new HttpProjectConfigManager(Period, Url, Logger);

                if (string.IsNullOrEmpty(SdkKey))
                    throw new Exception("SdkKey cannot be null");

                Url = string.Format(Format, SdkKey);
                var configManager = new HttpProjectConfigManager(Period, Url, Logger);

                if (Datafile != null)
                {
                    try
                    {
                        var config = HttpProjectConfigManager.ParseProjectConfig(Datafile);
                        configManager.SetConfig(config);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.WARN, "Error parsing fallback datafile." + ex.Message);
                    }
                }

                // Optionally block until config is available.
                if (!defer)
                    configManager.GetConfig();

                return configManager;
            }
        }
    }
}
