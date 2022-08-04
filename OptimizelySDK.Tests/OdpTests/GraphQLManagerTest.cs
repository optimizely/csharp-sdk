using Moq;
using NUnit.Framework;
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp;
using OptimizelySDK.Odp.Client;
using OptimizelySDK.Odp.Entity;
using System.Collections.Generic;

namespace OptimizelySDK.Tests.OdpTests
{
    [TestFixture]
    public class GraphQLManagerTest
    {
        private const string VALID_ODP_PUBLIC_KEY = "W4WzcEs-ABgXorzY7h1LCQ";

        private Mock<ILogger> _mockLogger;
        private Mock<IOdpClient> _mockOdpClient;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>();
            _mockLogger.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            _mockOdpClient = new Mock<IOdpClient>();
        }

        [Test]
        public void ShouldParseSuccessfulResponse()
        {
            #region const string responseJson

            var responseJson = @"
{
    ""data"": {
        ""customer"": {
            ""audiences"": {
                ""edges"": [
                {
                    ""node"": {
                        ""name"": ""has_email"",
                        ""state"": ""qualified"",
                    }
                },
                {
                    ""node"": {
                        ""name"": ""has_email_opted_in"",
                        ""state"": ""qualified"",
                    }
                },
              ]
            }
        }
    }
}";

            #endregion

            var manager = new GraphQLManager(_mockLogger.Object);

            var response = manager.ParseSegmentsResponseJson(responseJson);

            Assert.IsNull(response.Errors);
            Assert.IsNotNull(response.Data);
            Assert.IsNotNull(response.Data.Customer);
            Assert.IsNotNull(response.Data.Customer.Audiences);
            Assert.IsNotNull(response.Data.Customer.Audiences.Edges);
            Assert.IsTrue(response.Data.Customer.Audiences.Edges.Length == 2);
            var node = response.Data.Customer.Audiences.Edges[0].Node;
            Assert.IsTrue(node.Name == "has_email");
            Assert.IsTrue(node.State == BaseCondition.QUALIFIED);
            node = response.Data.Customer.Audiences.Edges[1].Node;
            Assert.IsTrue(node.Name == "has_email_opted_in");
            Assert.IsTrue(node.State == BaseCondition.QUALIFIED);
        }

        [Test]
        public void ShouldParseErrorResponse()
        {
            #region const string responseJson

            const string responseJson = @"
{
   ""errors"": [
        {
            ""message"": ""Exception while fetching data (/customer) : java.lang.RuntimeException: could not resolve _fs_user_id = asdsdaddddd"",
            ""locations"": [
                {
                    ""line"": 2,
                    ""column"": 3
                }
            ],
            ""path"": [
                ""customer""
            ],
            ""extensions"": {
                ""classification"": ""InvalidIdentifierException""
            }
        }
    ],
    ""data"": {
        ""customer"": null
    }
}";

            #endregion

            var manager = new GraphQLManager(_mockLogger.Object);

            var response = manager.ParseSegmentsResponseJson(responseJson);

            Assert.IsNull(response.Data.Customer);
            Assert.IsNotNull(response.Errors);
            Assert.IsTrue(response.Errors.Length == 1);
            var error = response.Errors[0];
            Assert.IsTrue(error.Message.Contains("asdsdaddddd"));
            Assert.IsTrue(error.Locations.Length == 1);
            var location = error.Locations[0];
            Assert.IsTrue(location.Line == 2);
            Assert.IsTrue(location.Column == 3);
            Assert.IsTrue(error.Path.Length == 1);
            Assert.IsTrue(error.Path[0] == "customer");
            Assert.IsTrue(error.Extensions.Classification == "InvalidIdentifierException");
        }

        [Test]
        public void ShouldFetchValidQualifiedSegments()
        {
            var expectedResponseJson = "{\"data\":{\"customer\":{\"audiences\":" +
                                       "{\"edges\":[{\"node\":{\"name\":\"has_email\"," +
                                       "\"state\":\"qualified\"}},{\"node\":{\"name\":" +
                                       "\"has_email_opted_in\",\"state\":\"qualified\"}}]}}}}";
            _mockOdpClient.Setup(
                    c => c.QuerySegments(It.IsAny<QuerySegmentsParameters>())).
                Returns(expectedResponseJson);
            var manager = new GraphQLManager(_mockLogger.Object, _mockOdpClient.Object);

            var segments = manager.FetchSegments(
                VALID_ODP_PUBLIC_KEY,
                "https://api.zaius.com/v3/graphql",
                "fs_user_id",
                "tester-101",
                new List<string>()
                {
                    "has_email",
                    "has_email_opted_in",
                    "push_on_sale",
                });

            Assert.IsTrue(segments.Length == 2);
            Assert.Contains("has_email", segments);
            Assert.Contains("has_email_opted_in", segments);
            _mockLogger.Verify(l => l.Log(LogLevel.WARN, It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ShouldHandleEmptyQualifiedSegments() { }

        [Test]
        public void ShouldHandleErrorWithInvalidIdentifier()
        {
            var expectedResponseJson = "{\"errors\":[{\"message\":" +
                                       "\"Exception while fetching data (/customer) : " +
                                       "java.lang.RuntimeException: could not resolve _fs_user_id = invalid-user\"," +
                                       "\"locations\":[{\"line\":1,\"column\":8}],\"path\":[\"customer\"]," +
                                       "\"extensions\":{\"classification\":\"DataFetchingException\"}}]," +
                                       "\"data\":{\"customer\":null}}";
            _mockOdpClient.Setup(
                    c => c.QuerySegments(It.IsAny<QuerySegmentsParameters>())).
                Returns(expectedResponseJson);
            var manager = new GraphQLManager(_mockLogger.Object);

            var segments = manager.FetchSegments(
                VALID_ODP_PUBLIC_KEY,
                "https://api.zaius.com/v3/graphql",
                "fs_user_id",
                "invalid-user", // invalid user
                new List<string>()
                {
                    "has_email",
                    "has_email_opted_in",
                    "push_on_sale",
                });

            Assert.IsTrue(segments.Length == 0);
            _mockLogger.Verify(l => l.Log(LogLevel.WARN, It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void ShouldHandleOtherException() { }

        [Test]
        public void ShouldHandleBadResponse() { }

        [Test]
        public void ShouldHandleNetworkError() { }

        [Test]
        public void ShouldHandle400HttpCode() { }

        [Test]
        public void ShouldHandle500HttpCode() { }
    }
}