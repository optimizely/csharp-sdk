using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizelySDK.Tests.OdpTests
{
    public static class HttpClientTestUtil
    {
        /// <summary>
        /// Create an HttpClient instance that returns an expected response
        /// </summary>
        /// <param name="statusCode">Http Status Code to return</param>
        /// <param name="content">Message body content to return</param>
        /// <returns>HttpClient instance that will return desired HttpResponseMessage</returns>
        public static HttpClient MakeHttpClient(HttpStatusCode statusCode,
            string content = default
        )
        {
            var response = new HttpResponseMessage(statusCode);
            
            if (!string.IsNullOrWhiteSpace(content))
            {
                response.Content = new StringContent(content);
            }

            var mockedHandler = new Mock<HttpMessageHandler>();
            mockedHandler.Protected().Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()).
                ReturnsAsync(response);
            return new HttpClient(mockedHandler.Object);
        }
    }
}
