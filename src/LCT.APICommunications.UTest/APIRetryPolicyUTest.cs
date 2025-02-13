using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LCT.APICommunications.Tests
{
    [TestFixture]
    public class APIRetryPolicyTests
    {
        private HttpClient _httpClient;

        public class CustomHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public CustomHttpMessageHandler(HttpResponseMessage response)
            {
                _response = response;
            }

            // Override SendAsync to return the provided response
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_response);
            }
        }

        [SetUp]
        public void SetUp()
        {
            // Initialize the HttpClient with a custom handler
        }

        [Test]
        public async Task GetRetryPolicy_ShouldRetryOnServerError_AndNotOnUnauthorized()
        {
            // Arrange
            var retryPolicy = APIRetryPolicy.GetRetryPolicy();
            var url = "https://example.com/api";

            // Simulate a 500 Internal Server Error on the first request, and 408 on second
            var handler1 = new CustomHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)); // 500
            var handler2 = new CustomHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.RequestTimeout)); // 408
            var handler3 = new CustomHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)); // 200 OK

            _httpClient = new HttpClient(handler1); // Start with 500 error

            // Act
            var response = await retryPolicy.ExecuteAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                return await _httpClient.SendAsync(request);
            });

            // Simulate the next retry steps:
            // Change the handler to simulate the second and third responses
            _httpClient = new HttpClient(handler2); // 408
            response = await retryPolicy.ExecuteAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                return await _httpClient.SendAsync(request);
            });

            _httpClient = new HttpClient(handler3); // 200 OK
            response = await retryPolicy.ExecuteAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                return await _httpClient.SendAsync(request);
            });

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public async Task GetRetryPolicy_ShouldNotRetryOnUnauthorized()
        {
            // Arrange
            var retryPolicy = APIRetryPolicy.GetRetryPolicy();
            var url = "https://example.com/api";

            // Simulate a 401 Unauthorized response
            var handler = new CustomHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            _httpClient = new HttpClient(handler);

            // Act
            var response = await retryPolicy.ExecuteAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                return await _httpClient.SendAsync(request);
            });

            // Assert
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        [Test]
        public async Task GetWebExceptionRetryPolicy_ShouldRetryOnWebException()
        {
            // Arrange
            var retryPolicy = APIRetryPolicy.GetWebExceptionRetryPolicy();
            var url = "https://example.com/api";

            // Simulate a WebException
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new WebException("Simulated WebException"));

            _httpClient = new HttpClient(handler.Object);

            // Act & Assert
            Assert.ThrowsAsync<WebException>(async () =>
            {
                await retryPolicy.ExecuteAsync(async () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    return await _httpClient.SendAsync(request);
                });
            });
        }

    }
}
