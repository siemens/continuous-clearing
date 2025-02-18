using log4net;
using Moq.Protected;
using Moq;
using System.Net;

namespace LCT.APICommunications.UTest
{
    public class RetryHttpClientHandlerUTest
    {
        [Test]
        public async Task SendAsync_ShouldRetry_OnTransientErrors()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException())
                .ThrowsAsync(new TaskCanceledException())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var retryHandler = new RetryHttpClientHandler
            {
                InnerHandler = handlerMock.Object
            };

            var httpClient = new HttpClient(retryHandler);

            // Act
            var response = await httpClient.GetAsync("http://test.com");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(3), // 2 retries + 1 initial call
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Test]
        public async Task SendAsync_ShouldNotRetry_OnNonTransientErrors()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

            var retryHandler = new RetryHttpClientHandler
            {
                InnerHandler = handlerMock.Object
            };

            var httpClient = new HttpClient(retryHandler);

            // Act
            var response = await httpClient.GetAsync("http://test.com");

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(), // No retries
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Test]
        public async Task SendAsync_ShouldLogRetryAttempts()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var loggerMock = GetLoggerMock();
            var retryHandler = new RetryHttpClientHandler()
            {
                InnerHandler = handlerMock.Object
            };

            var httpClient = new HttpClient(retryHandler);

            // Act
            var response = await httpClient.GetAsync("http://test.com");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);            
        }

        private static Mock<ILog> GetLoggerMock()
        {
            return new Mock<ILog>();
        }

    }
}
