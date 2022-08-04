/* 
 * Copyright 2022 Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Entity;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OptimizelySDK.Odp.Client
{
    public class OdpClient : IOdpClient
    {
        private readonly ILogger _logger;

        private readonly HttpClient _client;

        public OdpClient(ILogger logger, HttpClient client = null)
        {
            _logger = logger;
            _client = client ?? new HttpClient();
        }

        public string QuerySegments(QuerySegmentsParameters parameters)
        {
            return Task.Run(() => QuerySegmentsAsync(parameters)).GetAwaiter().GetResult();
        }

        private async Task<string> QuerySegmentsAsync(QuerySegmentsParameters parameters)
        {
            var request = BuildRequestMessage(parameters.ToJson(), parameters);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        private HttpRequestMessage BuildRequestMessage(string jsonQuery,
            QuerySegmentsParameters parameters
        )
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(parameters.ApiHost),
                Method = HttpMethod.Post,
                Headers =
                {
                    {
                        "x-api-key", parameters.ApiKey
                    },
                },
                Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json"),
            };

            return request;
        }
    }
}
