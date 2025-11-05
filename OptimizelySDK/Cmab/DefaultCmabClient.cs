/* 
* Copyright 2025, Optimizely
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
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Exceptions;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Cmab
{
    /// <summary>
    /// Default client for interacting with the CMAB service via HttpClient.
    /// </summary>
    public class DefaultCmabClient : ICmabClient
    {
        private readonly HttpClient _httpClient;
        private readonly CmabRetryConfig _retryConfig;
        private readonly ILogger _logger;
        private readonly IErrorHandler _errorHandler;
        private readonly string _predictionEndpointTemplate;

        public DefaultCmabClient(
            string predictionEndpointTemplate,
            HttpClient httpClient = null,
            CmabRetryConfig retryConfig = null,
            ILogger logger = null,
            IErrorHandler errorHandler = null)
        {
            _predictionEndpointTemplate = predictionEndpointTemplate;
            _httpClient = httpClient ?? new HttpClient();
            _retryConfig = retryConfig;
            _logger = logger ?? new NoOpLogger();
            _errorHandler = errorHandler ?? new NoOpErrorHandler();
        }

        private async Task<string> FetchDecisionAsync(
            string ruleId,
            string userId,
            IDictionary<string, object> attributes,
            string cmabUuid,
            TimeSpan? timeout = null)
        {
            var url = string.Format(_predictionEndpointTemplate, ruleId);
            var body = BuildRequestBody(ruleId, userId, attributes, cmabUuid);
            var perAttemptTimeout = timeout ?? CmabConstants.MAX_TIMEOUT;

            if (_retryConfig == null)
            {
                return await DoFetchOnceAsync(url, body, perAttemptTimeout).ConfigureAwait(false);
            }
            return await DoFetchWithRetryAsync(url, body, perAttemptTimeout).ConfigureAwait(false);
        }

        public string FetchDecision(
            string ruleId,
            string userId,
            IDictionary<string, object> attributes,
            string cmabUuid,
            TimeSpan? timeout = null)
        {
            try
            {
                return FetchDecisionAsync(ruleId, userId, attributes, cmabUuid, timeout).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex);
                throw;
            }
        }

        private static StringContent BuildContent(object payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            return new StringContent(json, Encoding.UTF8, CmabConstants.CONTENT_TYPE);
        }

        private static CmabRequest BuildRequestBody(string ruleId, string userId, IDictionary<string, object> attributes, string cmabUuid)
        {
            var attrList = new List<CmabAttribute>();

            if (attributes != null)
            {
                attrList = attributes.Select(kv => new CmabAttribute { Id = kv.Key, Value = kv.Value }).ToList();
            }

            return new CmabRequest
            {
                Instances = new List<CmabInstance>
                {
                    new CmabInstance
                    {
                        VisitorId = userId,
                        ExperimentId = ruleId,
                        Attributes = attrList,
                        CmabUUID = cmabUuid,
                    }
                }
            };
        }

        private async Task<string> DoFetchOnceAsync(string url, CmabRequest request, TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                try
                {
                    var httpRequest = new HttpRequestMessage
                    {
                        RequestUri = new Uri(url),
                        Method = HttpMethod.Post,
                        Content = BuildContent(request),
                    };

                    var response = await _httpClient.SendAsync(httpRequest, cts.Token).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        var status = (int)response.StatusCode;
                        _logger.Log(LogLevel.ERROR, string.Format(CmabConstants.ERROR_FETCH_FAILED_FMT, status));
                        throw new CmabFetchException(string.Format(CmabConstants.ERROR_FETCH_FAILED_FMT, status));
                    }

                    var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var j = JObject.Parse(responseText);
                    if (!ValidateResponse(j))
                    {
                        _logger.Log(LogLevel.ERROR, CmabConstants.ERROR_INVALID_RESPONSE);
                        throw new CmabInvalidResponseException(CmabConstants.ERROR_INVALID_RESPONSE);
                    }

                    var variationIdToken = j["predictions"][0]["variation_id"];
                    return variationIdToken?.ToString();
                }
                catch (JsonException ex)
                {
                    _logger.Log(LogLevel.ERROR, CmabConstants.ERROR_INVALID_RESPONSE);
                    throw new CmabInvalidResponseException(ex.Message);
                }
                catch (CmabInvalidResponseException)
                {
                    throw;
                }
                catch (HttpRequestException ex)
                {
                    _logger.Log(LogLevel.ERROR, string.Format(CmabConstants.ERROR_FETCH_FAILED_FMT, ex.Message));
                    throw new CmabFetchException(string.Format(CmabConstants.ERROR_FETCH_FAILED_FMT, ex.Message));
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.ERROR, string.Format(CmabConstants.ERROR_FETCH_FAILED_FMT, ex.Message));
                    throw new CmabFetchException(string.Format(CmabConstants.ERROR_FETCH_FAILED_FMT, ex.Message));
                }
            }
        }

        private async Task<string> DoFetchWithRetryAsync(string url, CmabRequest request, TimeSpan timeout)
        {
            var backoff = _retryConfig.InitialBackoff;
            var attempt = 0;
            while (true)
            {
                try
                {
                    return await DoFetchOnceAsync(url, request, timeout).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    if (attempt >= _retryConfig.MaxRetries)
                    {
                        _logger.Log(LogLevel.ERROR, string.Format(CmabConstants.ERROR_FETCH_FAILED_FMT, CmabConstants.EXHAUST_RETRY_MESSAGE));
                        throw new CmabFetchException(string.Format(CmabConstants.ERROR_FETCH_FAILED_FMT, CmabConstants.EXHAUST_RETRY_MESSAGE));
                    }

                    _logger.Log(LogLevel.INFO, $"Retrying CMAB request (attempt: {attempt + 1}) after {backoff.TotalSeconds} seconds...");
                    await Task.Delay(backoff).ConfigureAwait(false);
                    var nextMs = Math.Min(_retryConfig.MaxBackoff.TotalMilliseconds, backoff.TotalMilliseconds * _retryConfig.BackoffMultiplier);
                    backoff = TimeSpan.FromMilliseconds(nextMs);
                    attempt++;
                }
            }
        }

        private static bool ValidateResponse(JObject body)
        {
            if (body == null) return false;

            var preds = body["predictions"] as JArray;
            if (preds == null || preds.Count == 0) return false;

            var first = preds[0] as JObject;
            if (first == null) return false;

            return first["variation_id"] != null;
        }
    }
}
