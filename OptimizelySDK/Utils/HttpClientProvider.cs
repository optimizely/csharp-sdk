/*
 * Copyright 2023 Optimizely
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

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace OptimizelySDK.Utils
{
    /// <summary>
    /// Provides a singleton instance of HttpClient to prevent socket exhaustion.
    /// </summary>
    public class HttpClientProvider
    {
        private static readonly object lockObject = new object();
        private Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(CreateNewHttpClient);

        /// <summary>
        /// Gets a thread-safe, singleton instance of HttpClient.
        /// </summary>
        /// <returns>HttpClient instance</returns>
        public HttpClient GetClient()
        {
            if (IsDisposed(_httpClient.Value))
            {
                lock (lockObject)
                {
                    if (IsDisposed(_httpClient.Value))
                    {
                        _httpClient = new Lazy<HttpClient>(CreateNewHttpClient);
                    }
                }
            }

            return _httpClient.Value;
        }

        /// <summary>
        /// Creates a new HttpClient instance.
        /// </summary>
        /// <returns>HttpClient instance</returns>
        private static HttpClient CreateNewHttpClient()
        {
            return new HttpClient();
        }

        /// <summary>
        /// Checks if the HttpClient is disposed by attempting to access a property that
        /// will throw an exception if the client is disposed.
        /// </summary>
        /// <param name="httpClient">Client to check</param>
        /// <returns>True if the client is disposed, false otherwise.</returns>
        private static bool IsDisposed(HttpClient httpClient)
        {
            try
            {
                _ = httpClient.BaseAddress;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }

            return false;
        }
    }
}
