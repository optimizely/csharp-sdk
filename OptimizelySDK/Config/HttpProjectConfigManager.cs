/* 
 * Copyright 2019-2021, 2023 Optimizely
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

#if !(NET35 || NET40 || NETSTANDARD1_6)
#define USE_ODP
#endif

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;

namespace OptimizelySDK.Config
{
    public class HttpProjectConfigManager : PollingProjectConfigManager
    {
        private string Url;
        internal string LastModifiedSince = string.Empty;
        private string DatafileAccessToken = string.Empty;

        private HttpProjectConfigManager(TimeSpan period, string url, TimeSpan blockingTimeout,
            bool autoUpdate, ILogger logger, IErrorHandler errorHandler, string sdkKey
        )
            : base(period, blockingTimeout, autoUpdate, logger, errorHandler)
        {
            Url = url;
            SdkKey = sdkKey;
        }

        private HttpProjectConfigManager(TimeSpan period, string url, TimeSpan blockingTimeout,
            bool autoUpdate, ILogger logger, IErrorHandler errorHandler, string datafileAccessToken,
            string sdkKey
        )
            : this(period, url, blockingTimeout, autoUpdate, logger, errorHandler, sdkKey)
        {
            DatafileAccessToken = datafileAccessToken;
        }

        /// <summary>
        /// SDK key in use for this project
        /// </summary>
        public override string SdkKey { get; }

        public Task OnReady()
        {
            return CompletableConfigManager.Task;
        }

#if !NET40 && !NET35
        // HttpClient wrapper class which can be used to mock HttpClient for unit testing.
        public class HttpClient
        {
            private System.Net.Http.HttpClient Client;

            public HttpClient()
            {
                Client = new System.Net.Http.HttpClient(GetHttpClientHandler());
            }

            public HttpClient(System.Net.Http.HttpClient httpClient) : this()
            {
                if (httpClient != null)
                {
                    Client = httpClient;
                }
            }

            public static System.Net.Http.HttpClientHandler GetHttpClientHandler()
            {
                var handler = new System.Net.Http.HttpClientHandler()
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip |
                                             System.Net.DecompressionMethods.Deflate,
                };

                return handler;
            }

            public virtual Task<System.Net.Http.HttpResponseMessage> SendAsync(
                System.Net.Http.HttpRequestMessage httpRequestMessage
            )
            {
                return Client.SendAsync(httpRequestMessage);
            }
        }

        private static HttpClient Client;

        static HttpProjectConfigManager()
        {
            Client = new HttpClient();
        }

        private string GetRemoteDatafileResponse()
        {
            Logger.Log(LogLevel.DEBUG, $"Making datafile request to url \"{Url}\"");
            var request = new System.Net.Http.HttpRequestMessage
            {
                RequestUri = new Uri(Url),
                Method = System.Net.Http.HttpMethod.Get,
            };

            // Send If-Modified-Since header if Last-Modified-Since header contains any value.
            if (!string.IsNullOrEmpty(LastModifiedSince))
            {
                request.Headers.Add("If-Modified-Since", LastModifiedSince);
                Logger.Log(LogLevel.DEBUG,
                    $"Set If-Modified-Since in request header: {LastModifiedSince}");
            }

            if (!string.IsNullOrEmpty(DatafileAccessToken))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
                        DatafileAccessToken);
            }

            var httpResponse = Client.SendAsync(request);
            httpResponse.Wait();

            // Return from here if datafile is not modified.
            var result = httpResponse.Result;

            if (result.StatusCode == HttpStatusCode.NotModified)
            {
                return null;
            }

            if (!result.IsSuccessStatusCode)
            {
                Logger.Log(LogLevel.ERROR, $"Error fetching datafile \"{result.StatusCode}\"");
                return null;
            }

            // Update Last-Modified header if provided.
            if (result.Content.Headers.LastModified.HasValue)
            {
                LastModifiedSince = result.Content.Headers.LastModified?.UtcDateTime.ToString("r");
                Logger.Log(LogLevel.DEBUG,
                    $"Set LastModifiedSince from response header: {LastModifiedSince}");
            }

            var content = result.Content.ReadAsStringAsync();
            content.Wait();

            return content.Result;
        }
#elif NET40
        private string GetRemoteDatafileResponse()
        {
            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(Url);

            // Send If-Modified-Since header if Last-Modified-Since header contains any value.
            if (!string.IsNullOrEmpty(LastModifiedSince))
            {
                request.Headers.Add("If-Modified-Since", LastModifiedSince);
            }

            var result = (System.Net.HttpWebResponse)request.GetResponse();

            if (result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Logger.Log(LogLevel.ERROR, $"Error fetching datafile \"{result.StatusCode}\"");
            }

            var lastModified = result.Headers.GetValues("Last-Modified");
            if (!string.IsNullOrEmpty(lastModified?.First()))
            {
                LastModifiedSince = lastModified.First();
            }

            var encoding = System.Text.Encoding.ASCII;
            using (var reader = new System.IO.StreamReader(result.GetResponseStream(), encoding))
            {
                var responseText = reader.ReadToEnd();
                return responseText;
            }
        }
#else
        private string GetRemoteDatafileResponse()
        {
            return null;
        }
#endif
        protected override ProjectConfig Poll()
        {
            var datafile = GetRemoteDatafileResponse();

            if (datafile == null)
            {
                return null;
            }

            return DatafileProjectConfig.Create(datafile, Logger, ErrorHandler);
        }

        public class Builder
        {
            private const long MAX_MILLISECONDS_LIMIT = 4294967294;
            private readonly TimeSpan DEFAULT_PERIOD = TimeSpan.FromMinutes(5);
            private readonly TimeSpan DEFAULT_BLOCKINGOUT_PERIOD = TimeSpan.FromSeconds(15);

            private readonly string DEFAULT_FORMAT =
                "https://cdn.optimizely.com/datafiles/{0}.json";

            private readonly string DEFAULT_AUTHENTICATED_DATAFILE_FORMAT =
                "https://config.optimizely.com/datafiles/auth/{0}.json";

            private string Datafile;
            private string DatafileAccessToken;
            private string SdkKey;
            private string Url;
            private string Format;
            private ILogger Logger;
            private IErrorHandler ErrorHandler;
            private TimeSpan Period;
            private TimeSpan BlockingTimeoutSpan;
            private bool AutoUpdate = true;
            private bool StartByDefault = true;
            private NotificationCenter NotificationCenter;

            private bool IsBlockingTimeoutProvided = false;
            private bool IsPollingIntervalProvided = false;

            public Builder WithBlockingTimeoutPeriod(TimeSpan blockingTimeoutSpan)
            {
                IsBlockingTimeoutProvided = true;

                BlockingTimeoutSpan = blockingTimeoutSpan;

                return this;
            }

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
#if !NET40 && !NET35
            public Builder WithAccessToken(string accessToken)
            {
                DatafileAccessToken = accessToken;

                return this;
            }
#endif

            public Builder WithUrl(string url)
            {
                Url = url;

                return this;
            }

            public Builder WithPollingInterval(TimeSpan period)
            {
                IsPollingIntervalProvided = true;

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

            public Builder WithErrorHandler(IErrorHandler errorHandler)
            {
                ErrorHandler = errorHandler;

                return this;
            }

            public Builder WithAutoUpdate(bool autoUpdate)
            {
                AutoUpdate = autoUpdate;

                return this;
            }

            public Builder WithStartByDefault(bool startByDefault = true)
            {
                StartByDefault = startByDefault;

                return this;
            }

            public Builder WithNotificationCenter(NotificationCenter notificationCenter)
            {
                NotificationCenter = notificationCenter;

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
                HttpProjectConfigManager configManager = null;

                if (Logger == null)
                {
                    Logger = new DefaultLogger();
                }

                if (ErrorHandler == null)
                {
                    ErrorHandler = new DefaultErrorHandler(Logger, false);
                }

                if (string.IsNullOrEmpty(Format))
                {
                    if (string.IsNullOrEmpty(DatafileAccessToken))
                    {
                        Format = DEFAULT_FORMAT;
                    }
                    else
                    {
                        Format = DEFAULT_AUTHENTICATED_DATAFILE_FORMAT;
                    }
                }

                if (string.IsNullOrEmpty(Url))
                {
                    if (string.IsNullOrEmpty(SdkKey))
                    {
                        ErrorHandler.HandleError(new Exception("SdkKey cannot be null"));
                    }

                    Url = string.Format(Format, SdkKey);
                }

                if (IsPollingIntervalProvided && (Period.TotalMilliseconds <= 0 ||
                                                  Period.TotalMilliseconds >
                                                  MAX_MILLISECONDS_LIMIT))
                {
                    Logger.Log(LogLevel.DEBUG,
                        $"Polling interval is not valid for periodic calls, using default period {DEFAULT_PERIOD.TotalMilliseconds}ms");
                    Period = DEFAULT_PERIOD;
                }
                else if (!IsPollingIntervalProvided)
                {
                    Logger.Log(LogLevel.DEBUG,
                        $"No polling interval provided, using default period {DEFAULT_PERIOD.TotalMilliseconds}ms");
                    Period = DEFAULT_PERIOD;
                }


                if (IsBlockingTimeoutProvided && (BlockingTimeoutSpan.TotalMilliseconds <= 0 ||
                                                  BlockingTimeoutSpan.TotalMilliseconds >
                                                  MAX_MILLISECONDS_LIMIT))
                {
                    Logger.Log(LogLevel.DEBUG,
                        $"Blocking timeout is not valid, using default blocking timeout {DEFAULT_BLOCKINGOUT_PERIOD.TotalMilliseconds}ms");
                    BlockingTimeoutSpan = DEFAULT_BLOCKINGOUT_PERIOD;
                }
                else if (!IsBlockingTimeoutProvided)
                {
                    Logger.Log(LogLevel.DEBUG,
                        $"No Blocking timeout provided, using default blocking timeout {DEFAULT_BLOCKINGOUT_PERIOD.TotalMilliseconds}ms");
                    BlockingTimeoutSpan = DEFAULT_BLOCKINGOUT_PERIOD;
                }


                configManager = new HttpProjectConfigManager(Period, Url, BlockingTimeoutSpan,
                    AutoUpdate, Logger, ErrorHandler, DatafileAccessToken, SdkKey);

                if (Datafile != null)
                {
                    try
                    {
                        var config = DatafileProjectConfig.Create(Datafile, Logger, ErrorHandler);
                        configManager.SetConfig(config);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.WARN, "Error parsing fallback datafile." + ex.Message);
                    }
                }

                configManager.NotifyOnProjectConfigUpdate += () =>
                {
#if USE_ODP
                    NotificationCenterRegistry.GetNotificationCenter(SdkKey)
                        .SendNotifications(
                            NotificationCenter.NotificationType.OptimizelyConfigUpdate);
#endif
                    NotificationCenter?.SendNotifications(NotificationCenter.NotificationType
                        .OptimizelyConfigUpdate);
                };


                if (StartByDefault)
                {
                    configManager.Start();
                }

                // Optionally block until config is available.
                if (!defer)
                {
                    configManager.GetConfig();
                }

                return configManager;
            }
        }
    }
}
