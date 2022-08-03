using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Entity;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace OptimizelySDK.Odp.Client
{
    public class HttpClientOdpClient : IOdpClient
    {
        public ILogger Logger { get; set; } = new DefaultLogger();

        private static readonly HttpClient Client;

        public static HttpClientOdpClient()
        {
            Client = new HttpClient();
        }

        public string QuerySegments(QuerySegmentsParameters parameters)
        {
           return Task.Run(() => QuerySegmentsAsync(parameters)).GetAwaiter().GetResult();
        }

        private async Task<string> QuerySegmentsAsync(QuerySegmentsParameters parameters)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(parameters.ApiHost),
                Method = HttpMethod.Post,
                Headers =
                {
                    {
                        "Content-Type", "application/json"
                    },
                    {
                        "x-api-key", parameters.ApiKey
                    },
                },
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            };

            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}