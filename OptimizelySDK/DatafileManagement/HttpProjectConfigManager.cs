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
    public class HttpProjectConfigManager : ProjectConfigManager
    {
        private string Url;
        public HttpClient Client;
        ProjectConfig ProjectConfig;
        protected ILogger Logger { get; set; }
        private string LastModifiedSince = string.Empty;
        
        private HttpProjectConfigManager(string url, ILogger logger)
        {
            Client = new HttpClient();
            Url = url;
            Logger = logger;
        }

        public ProjectConfig GetConfig()
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
                return ProjectConfig;

            // Update Last-Modified header if provided.
            if (response.Headers.TryGetValues("Last-Modified", out IEnumerable<string> values))
                LastModifiedSince = values.First();

            var content = response.Content.ReadAsStringAsync();
            content.Wait();
            
            string datafile = content.Result.ToString();
            var projectConfig = DatafileProjectConfig.Create(datafile, null, null);

            SetConfig(projectConfig);
            return projectConfig;
        }

        public bool SetConfig(ProjectConfig projectConfig)
        {
            if (projectConfig != null)
            {
                ProjectConfig = projectConfig;
                return true;
            }

            return false;
        }

        public class Builder
        {
            private string SdkKey;
            private string Url;
            private string Format = "https://cdn.optimizely.com/datafiles/{0}.json";
            private ILogger Logger;

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
            
            public HttpProjectConfigManager Build()
            {
                if (Logger == null)
                    Logger = new DefaultLogger();

                if (!string.IsNullOrEmpty(Url))
                    return new HttpProjectConfigManager(Url, Logger);

                if (string.IsNullOrEmpty(SdkKey))
                    throw new Exception("SdkKey cannot be null");

                Url = string.Format(Format, SdkKey);
                return new HttpProjectConfigManager(Url, Logger);
            }
        }
    }
}
