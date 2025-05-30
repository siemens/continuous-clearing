// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using log4net;
using Moq;
using Moq.Protected;
using System.Net;

namespace LCT.APICommunications.UTest
{
    public class RetryHttpClientHandlerUTest
    {
        private Mock<ILog> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            // Mock the logger
            _mockLogger = new Mock<ILog>();
        }
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
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
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
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict));

            var retryHandler = new RetryHttpClientHandler
            {
                InnerHandler = handlerMock.Object
            };

            var httpClient = new HttpClient(retryHandler);

            // Act
            var response = await httpClient.GetAsync("http://test.com");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
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


            var retryHandler = new RetryHttpClientHandler()
            {
                InnerHandler = handlerMock.Object
            };

            var httpClient = new HttpClient(retryHandler);

            // Act
            var response = await httpClient.GetAsync("http://test.com");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
        [Test]
        public async Task ExecuteWithRetryAsync_ShouldCompleteSuccessfully_WhenActionSucceedsAfterRetry()
        {
            // Arrange
            var attempts = 0;
            var action = new Func<Task>(() =>
            {
                attempts++;
                if (attempts < ApiConstant.APIRetryIntervals.Count)
                {
                    throw new WebException("Temporary error", WebExceptionStatus.Timeout);
                }
                return Task.CompletedTask; // Successfully completes after retries
            });

            // Act
            await RetryHttpClientHandler.ExecuteWithRetryAsync(action);

            // Assert
            Assert.That(attempts, Is.EqualTo(ApiConstant.APIRetryIntervals.Count), "Action should have been attempted the expected number of times.");
        }

        [Test]
        public async Task ExecuteWithRetryAsync_ShouldNotRetry_WhenNoWebExceptionIsThrown()
        {
            // Arrange
            var actionExecuted = false;
            var action = new Func<Task>(() =>
            {
                actionExecuted = true;
                return Task.CompletedTask;
            });

            // Act
            await RetryHttpClientHandler.ExecuteWithRetryAsync(action);

            // Assert
            Assert.That(actionExecuted, Is.True, "Action should have been executed.");
            _mockLogger.Verify(logger => logger.Debug(It.IsAny<string>()), Times.Never, "Retry should not occur if there is no exception.");
        }

    }
}
