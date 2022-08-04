using Newtonsoft.Json;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Entity;
using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OptimizelySDK.Odp.Client
{
    public class HttpClientOdpClient : IOdpClient
    {
        public ILogger Logger { get; set; } = new DefaultLogger();

        private readonly HttpClient Client = new HttpClient();

        public string QuerySegments(QuerySegmentsParameters parameters)
        {
           return Task.Run(() => QuerySegmentsAsync(parameters)).GetAwaiter().GetResult();
        }

        private async Task<string> QuerySegmentsAsync(QuerySegmentsParameters parameters)
        {
            var request = BuildRequestMessage(parameters.ToJson(), parameters);

            var response = await Client.SendAsync(request);
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