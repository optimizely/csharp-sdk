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

            try
            {
                var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.WARN, $"Unable to query ODP: {ex.Message}");
                return string.Empty;
            }
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