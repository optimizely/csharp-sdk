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

using Moq;
using NUnit.Framework;
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp;
using System.Collections.Generic;
using System.Net;

namespace OptimizelySDK.Tests.OdpTests
{
    [TestFixture]
    public class GraphQLManagerTest
    {
        private const string VALID_ODP_PUBLIC_KEY = "not-real-odp-public-key";
        private const string ODP_GRAPHQL_HOST = "https://graph.example.com";

        private readonly List<string> _segmentsToCheck = new List<string>
        {
            "has_email",
            "has_email_opted_in",
            "push_on_sale",
        };

        private Mock<IErrorHandler> _mockErrorHandler;
        private Mock<ILogger> _mockLogger;

        [SetUp]
        public void Setup()
        {
            _mockErrorHandler = new Mock<IErrorHandler>();
            _mockLogger = new Mock<ILogger>();
            _mockLogger.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
        }

        [Test]
        public void ShouldParseSuccessfulResponse()
        {
            const string RESPONSE_JSON = @"
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
                        ""state"": ""not-qualified""
                    }
                },
              ]
            },
        }
    }
}";

            var response = GraphQLManager.ParseSegmentsResponseJson(RESPONSE_JSON);

            Assert.IsNull(response.Errors);
            Assert.IsNotNull(response.Data);
            Assert.IsNotNull(response.Data.Customer);
            Assert.IsNotNull(response.Data.Customer.Audiences);
            Assert.IsNotNull(response.Data.Customer.Audiences.Edges);
            Assert.IsTrue(response.Data.Customer.Audiences.Edges.Length == 2);
            var node = response.Data.Customer.Audiences.Edges[0].Node;
            Assert.AreEqual(node.Name, "has_email");
            Assert.AreEqual(node.State, BaseCondition.QUALIFIED);
            node = response.Data.Customer.Audiences.Edges[1].Node;
            Assert.AreEqual(node.Name, "has_email_opted_in");
            Assert.AreNotEqual(node.State, BaseCondition.QUALIFIED);
        }

        [Test]
        public void ShouldParseErrorResponse()
        {
            const string RESPONSE_JSON = @"
{
   ""errors"": [
        {
            ""message"": ""Exception while fetching data (/customer) : Exception: could not resolve _fs_user_id = asdsdaddddd"",
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

            var response = GraphQLManager.ParseSegmentsResponseJson(RESPONSE_JSON);

            Assert.IsNull(response.Data.Customer);
            Assert.IsNotNull(response.Errors);
            Assert.AreEqual(response.Errors[0].Extensions.Classification,
                "InvalidIdentifierException");
        }

        [Test]
        public void ShouldFetchValidQualifiedSegments()
        {
            const string RESPONSE_DATA = "{\"data\":{\"customer\":{\"audiences\":" +
                                         "{\"edges\":[{\"node\":{\"name\":\"has_email\"," +
                                         "\"state\":\"qualified\"}},{\"node\":{\"name\":" +
                                         "\"has_email_opted_in\",\"state\":\"qualified\"}}]}}}}";
            var httpClient = HttpClientTestUtil.MakeHttpClient(HttpStatusCode.OK, RESPONSE_DATA);
            var manager =
                new GraphQLManager(_mockLogger.Object, _mockErrorHandler.Object, httpClient);

            var segments = manager.FetchSegments(
                VALID_ODP_PUBLIC_KEY,
                ODP_GRAPHQL_HOST,
                OdpUserKeyType.FS_USER_KEY,
                "tester-101",
                _segmentsToCheck);

            Assert.IsTrue(segments.Length == 2);
            Assert.Contains("has_email", segments);
            Assert.Contains("has_email_opted_in", segments);
            _mockLogger.Verify(l => l.Log(LogLevel.WARN, It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ShouldHandleEmptyQualifiedSegments()
        {
            const string RESPONSE_DATA = "{\"data\":{\"customer\":{\"audiences\":" +
                                         "{\"edges\":[ ]}}}}";
            var httpClient = HttpClientTestUtil.MakeHttpClient(HttpStatusCode.OK, RESPONSE_DATA);
            var manager =
                new GraphQLManager(_mockLogger.Object, _mockErrorHandler.Object, httpClient);

            var segments = manager.FetchSegments(
                VALID_ODP_PUBLIC_KEY,
                ODP_GRAPHQL_HOST,
                OdpUserKeyType.FS_USER_KEY,
                "tester-101",
                _segmentsToCheck);

            Assert.IsTrue(segments.Length == 0);
            _mockLogger.Verify(l => l.Log(LogLevel.WARN, It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ShouldHandleErrorWithInvalidIdentifier()
        {
            const string RESPONSE_DATA = "{\"errors\":[{\"message\":" +
                                         "\"Exception while fetching data (/customer) : " +
                                         "Exception: could not resolve _fs_user_id = invalid-user\"," +
                                         "\"locations\":[{\"line\":1,\"column\":8}],\"path\":[\"customer\"]," +
                                         "\"extensions\":{\"classification\":\"DataFetchingException\"}}]," +
                                         "\"data\":{\"customer\":null}}";
            var httpClient = HttpClientTestUtil.MakeHttpClient(HttpStatusCode.OK, RESPONSE_DATA);
            var manager =
                new GraphQLManager(_mockLogger.Object, _mockErrorHandler.Object, httpClient);

            var segments = manager.FetchSegments(
                VALID_ODP_PUBLIC_KEY,
                ODP_GRAPHQL_HOST,
                OdpUserKeyType.FS_USER_KEY,
                "invalid-user",
                _segmentsToCheck);

            Assert.IsNull(segments);
            _mockLogger.Verify(l => l.Log(LogLevel.ERROR, It.IsAny<string>()),
                Times.Once);
        }

        [Test]
        public void ShouldHandleBadResponse()
        {
            const string RESPONSE_DATA = "{\"data\":{ }}";
            var httpClient = HttpClientTestUtil.MakeHttpClient(HttpStatusCode.OK, RESPONSE_DATA);
            var manager =
                new GraphQLManager(_mockLogger.Object, _mockErrorHandler.Object, httpClient);

            var segments = manager.FetchSegments(
                VALID_ODP_PUBLIC_KEY,
                ODP_GRAPHQL_HOST,
                OdpUserKeyType.FS_USER_KEY,
                "tester-101",
                _segmentsToCheck);

            Assert.IsNull(segments);
            _mockLogger.Verify(
                l => l.Log(LogLevel.ERROR, "Audience segments fetch failed (decode error)"),
                Times.Once);
        }

        [Test]
        public void ShouldHandleUnrecognizedJsonResponse()
        {
            const string RESPONSE_DATA =
                "{\"unExpectedObject\":{ \"withSome\": \"value\", \"thatIsNotParseable\": \"true\" }}";
            var httpClient = HttpClientTestUtil.MakeHttpClient(HttpStatusCode.OK, RESPONSE_DATA);
            var manager =
                new GraphQLManager(_mockLogger.Object, _mockErrorHandler.Object, httpClient);

            var segments = manager.FetchSegments(
                VALID_ODP_PUBLIC_KEY,
                ODP_GRAPHQL_HOST,
                OdpUserKeyType.FS_USER_KEY,
                "tester-101",
                _segmentsToCheck);

            Assert.IsNull(segments);
            _mockLogger.Verify(
                l => l.Log(LogLevel.ERROR, "Audience segments fetch failed (decode error)"),
                Times.Once);
        }

        [Test]
        public void ShouldHandle400HttpCode()
        {
            var httpClient = HttpClientTestUtil.MakeHttpClient(HttpStatusCode.BadRequest);
            var manager =
                new GraphQLManager(_mockLogger.Object, _mockErrorHandler.Object, httpClient);

            var segments = manager.FetchSegments(
                VALID_ODP_PUBLIC_KEY,
                ODP_GRAPHQL_HOST,
                OdpUserKeyType.FS_USER_KEY,
                "tester-101",
                _segmentsToCheck);

            Assert.IsNull(segments);
            _mockLogger.Verify(l => l.Log(LogLevel.ERROR, "Audience segments fetch failed (400)"),
                Times.Once);
        }

        [Test]
        public void ShouldHandle500HttpCode()
        {
            var httpClient = HttpClientTestUtil.MakeHttpClient(HttpStatusCode.InternalServerError);
            var manager =
                new GraphQLManager(_mockLogger.Object, _mockErrorHandler.Object, httpClient);
            
            var segments = manager.FetchSegments(
                VALID_ODP_PUBLIC_KEY,
                ODP_GRAPHQL_HOST,
                OdpUserKeyType.FS_USER_KEY,
                "tester-101",
                _segmentsToCheck);

            Assert.IsNull(segments);
            _mockLogger.Verify(l => l.Log(LogLevel.ERROR, "Audience segments fetch failed (500)"),
                Times.Once);
        }
    }
}
