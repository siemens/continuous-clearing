// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using Moq;
using Moq.Protected;
using NUnit.Framework;
using SW360KeycloakService.Interfaces;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SW360KeycloakService.UTest
{
    [TestFixture]
    public class TokenRefreshDelegatingHandlerUTest
    {
        private const string ValidToken = "eyJhbGciOiJSUzI1NiJ9.valid_token";
        private const string RefreshedToken = "eyJhbGciOiJSUzI1NiJ9.refreshed_token";

        // ─── Scenario 1: Non-401 response passes through unchanged ────────────────

        [Test]
        public async Task SendAsync_ShouldPassThrough_WhenResponseIsNotUnauthorized()
        {
            // Arrange
            var tokenServiceMock = new Mock<IKeycloakTokenService>();
            var innerHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            innerHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var handler = new TokenRefreshDelegatingHandler(tokenServiceMock.Object)
            {
                InnerHandler = innerHandlerMock.Object
            };
            var httpClient = new HttpClient(handler);

            // Act
            var response = await httpClient.GetAsync("http://sw360.test/api");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            tokenServiceMock.Verify(s => s.InvalidateToken(), Times.Never);
            tokenServiceMock.Verify(s => s.GetOrRefreshTokenAsync(), Times.Never);
        }

        // ─── Scenario 2: 401 triggers token refresh and request retry ────────────

        [Test]
        public async Task SendAsync_ShouldRefreshTokenAndRetry_On401()
        {
            // Arrange
            var tokenServiceMock = new Mock<IKeycloakTokenService>();
            tokenServiceMock.Setup(s => s.GetOrRefreshTokenAsync()).ReturnsAsync(RefreshedToken);

            var innerHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            innerHandlerMock
                .Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized))  // 1st attempt
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));           // retry

            var handler = new TokenRefreshDelegatingHandler(tokenServiceMock.Object)
            {
                InnerHandler = innerHandlerMock.Object
            };
            var httpClient = new HttpClient(handler);

            // Act
            var response = await httpClient.GetAsync("http://sw360.test/api");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            tokenServiceMock.Verify(s => s.InvalidateToken(), Times.Once);
            tokenServiceMock.Verify(s => s.GetOrRefreshTokenAsync(), Times.Once);
        }

        // ─── Scenario 3: Refreshed token is set on retry request header ──────────

        [Test]
        public async Task SendAsync_ShouldSetNewBearerToken_OnRetryRequest()
        {
            // Arrange
            var tokenServiceMock = new Mock<IKeycloakTokenService>();
            tokenServiceMock.Setup(s => s.GetOrRefreshTokenAsync()).ReturnsAsync(RefreshedToken);

            AuthenticationHeaderValue capturedHeader = null;
            var innerHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            int callCount = 0;
            innerHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((req, _) =>
                {
                    callCount++;
                    if (callCount == 1)
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                    capturedHeader = req.Headers.Authorization;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                });

            var handler = new TokenRefreshDelegatingHandler(tokenServiceMock.Object)
            {
                InnerHandler = innerHandlerMock.Object
            };
            var httpClient = new HttpClient(handler);

            // Act
            await httpClient.GetAsync("http://sw360.test/api");

            // Assert — retry request has the new Bearer token
            Assert.That(capturedHeader, Is.Not.Null);
            Assert.That(capturedHeader.Scheme, Is.EqualTo("Bearer"));
            Assert.That(capturedHeader.Parameter, Is.EqualTo(RefreshedToken));
        }

        // ─── Scenario 4: Empty refresh token returns original 401 ────────────────

        [Test]
        public async Task SendAsync_ShouldReturnOriginal401_WhenRefreshedTokenIsEmpty()
        {
            // Arrange
            var tokenServiceMock = new Mock<IKeycloakTokenService>();
            tokenServiceMock.Setup(s => s.GetOrRefreshTokenAsync()).ReturnsAsync(string.Empty);

            var innerHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            innerHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            var handler = new TokenRefreshDelegatingHandler(tokenServiceMock.Object)
            {
                InnerHandler = innerHandlerMock.Object
            };
            var httpClient = new HttpClient(handler);

            // Act
            var response = await httpClient.GetAsync("http://sw360.test/api");

            // Assert — original 401 returned, no retry
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            innerHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),                                            // only 1 call, no retry
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        // ─── Scenario 5: Null refresh token returns original 401 ─────────────────

        [Test]
        public async Task SendAsync_ShouldReturnOriginal401_WhenRefreshedTokenIsNull()
        {
            // Arrange
            var tokenServiceMock = new Mock<IKeycloakTokenService>();
            tokenServiceMock.Setup(s => s.GetOrRefreshTokenAsync()).ReturnsAsync((string)null);

            var innerHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            innerHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            var handler = new TokenRefreshDelegatingHandler(tokenServiceMock.Object)
            {
                InnerHandler = innerHandlerMock.Object
            };
            var httpClient = new HttpClient(handler);

            // Act
            var response = await httpClient.GetAsync("http://sw360.test/api");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        // ─── Scenario 6: Only one retry on 401, not infinite loop ────────────────

        [Test]
        public async Task SendAsync_ShouldRetryOnlyOnce_EvenIfSecondAttemptAlso401()
        {
            // Arrange
            var tokenServiceMock = new Mock<IKeycloakTokenService>();
            tokenServiceMock.Setup(s => s.GetOrRefreshTokenAsync()).ReturnsAsync(RefreshedToken);

            var innerHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            innerHandlerMock
                .Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized))  // 1st attempt → 401
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized)); // retry → still 401

            var handler = new TokenRefreshDelegatingHandler(tokenServiceMock.Object)
            {
                InnerHandler = innerHandlerMock.Object
            };
            var httpClient = new HttpClient(handler);

            // Act
            var response = await httpClient.GetAsync("http://sw360.test/api");

            // Assert — returns the 401 from retry, does NOT retry again
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            innerHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),                                        // exactly 2 attempts total
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
            tokenServiceMock.Verify(s => s.InvalidateToken(), Times.Once);          // only one refresh
        }
    }
}
