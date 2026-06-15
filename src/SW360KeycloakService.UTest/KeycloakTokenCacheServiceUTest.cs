// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using Moq;
using Moq.Protected;
using NUnit.Framework;
using SW360KeycloakService.Model;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SW360KeycloakService.UTest
{
    [TestFixture]
    public class KeycloakTokenCacheServiceUTest
    {
        private const string ValidToken = "eyJhbGciOiJSUzI1NiJ9.test_token";
        private const string NewToken = "eyJhbGciOiJSUzI1NiJ9.new_token";

        private const string StaticFallbackToken = "static-fallback-token";

        private TokenServiceSettings BuildSettingsWithFallback(string clientId = "test-client", string secret = "test-secret", string url = "https://sw360.example.com") =>
            new TokenServiceSettings
            {
                SW360BaseUrl = url,
                ClientId = clientId,
                ClientSecret = secret,
                KeyCloakToken = StaticFallbackToken,
                KeyCloakTokenType = "Bearer"
            };

        private TokenServiceSettings BuildSettings(string clientId = "test-client", string secret = "test-secret", string url = "https://sw360.example.com")
        {
            return new TokenServiceSettings
            {
                SW360BaseUrl = url,
                ClientId = clientId,
                ClientSecret = secret,
                KeyCloakToken = null,
                KeyCloakTokenType = "Bearer"
            };
        }

        private HttpClient BuildMockHttpClient(HttpStatusCode statusCode, string responseBody, Mock<HttpMessageHandler> handlerMock = null)
        {
            handlerMock ??= new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(responseBody)
                });
            return new HttpClient(handlerMock.Object);
        }

        private static string TokenJson(string token = ValidToken, int expiresIn = 36000) =>
            $"{{\"access_token\":\"{token}\",\"token_type\":\"Bearer\",\"expires_in\":{expiresIn}}}";

        // ─── Scenario 1: Successful token fetch and caching ───────────────────────

        [Test]
        public async Task GetOrRefreshTokenAsync_ShouldFetchAndCacheToken_OnFirstCall()
        {
            // Arrange
            var settings = BuildSettings();
            var httpClient = BuildMockHttpClient(HttpStatusCode.OK, TokenJson());
            var sut = new KeycloakTokenCacheService(settings, _ => { }, httpClient);

            // Act
            string token = await sut.GetOrRefreshTokenAsync();

            // Assert
            Assert.That(token, Is.EqualTo(ValidToken));
            Assert.That(settings.KeyCloakToken, Is.EqualTo(ValidToken)); // synced back
        }

        // ─── Scenario 2: Cached token returned without HTTP call ─────────────────

        [Test]
        public async Task GetOrRefreshTokenAsync_ShouldReturnCachedToken_WithoutHttpCall()
        {
            // Arrange
            var settings = BuildSettings();
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(TokenJson())
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var sut = new KeycloakTokenCacheService(settings, _ => { }, httpClient);

            // Act — call twice
            await sut.GetOrRefreshTokenAsync();
            string secondToken = await sut.GetOrRefreshTokenAsync();

            // Assert — only one HTTP call made
            Assert.That(secondToken, Is.EqualTo(ValidToken));
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        // ─── Scenario 3: InvalidateToken forces re-fetch ──────────────────────────

        [Test]
        public async Task GetOrRefreshTokenAsync_ShouldRefetchToken_AfterInvalidateToken()
        {
            // Arrange
            var settings = BuildSettings();
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(TokenJson(ValidToken)) })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(TokenJson(NewToken)) });

            var httpClient = new HttpClient(handlerMock.Object);
            var sut = new KeycloakTokenCacheService(settings, _ => { }, httpClient);

            // Act
            string first = await sut.GetOrRefreshTokenAsync();
            sut.ClearOldCacheToken();                                        // ← simulate 401 invalidation
            string second = await sut.GetOrRefreshTokenAsync();

            // Assert
            Assert.That(first, Is.EqualTo(ValidToken));
            Assert.That(second, Is.EqualTo(NewToken));                   // fresh token fetched
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        // ─── Scenario 4: 401 from Keycloak calls exitAction ──────────────────────

        [Test]
        public async Task GetOrRefreshTokenAsync_ShouldCallExitAction_When401FromKeycloak()
        {
            // Arrange
            var settings = BuildSettings();
            var httpClient = BuildMockHttpClient(HttpStatusCode.Unauthorized,
                "{\"error\":\"invalid_client\",\"error_description\":\"Invalid client credentials\"}");
            int exitCode = 0;
            var sut = new KeycloakTokenCacheService(settings, code => exitCode = code, httpClient);

            // Act
            await sut.GetOrRefreshTokenAsync();

            // Assert
            Assert.That(exitCode, Is.EqualTo(-1));
        }

        // ─── Scenario 5: Network error calls exitAction ───────────────────────────

        [Test]
        public async Task GetOrRefreshTokenAsync_ShouldCallExitAction_OnNetworkError()
        {
            // Arrange
            var settings = BuildSettings();
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            var httpClient = new HttpClient(handlerMock.Object);
            int exitCode = 0;
            var sut = new KeycloakTokenCacheService(settings, code => exitCode = code, httpClient);

            // Act
            await sut.GetOrRefreshTokenAsync();

            // Assert
            Assert.That(exitCode, Is.EqualTo(-1));
        }

        // ─── Scenario 6: Timeout calls exitAction ────────────────────────────────

        [Test]
        public async Task GetOrRefreshTokenAsync_ShouldCallExitAction_OnTimeout()
        {
            // Arrange
            var settings = BuildSettings();
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Request timed out"));

            var httpClient = new HttpClient(handlerMock.Object);
            int exitCode = 0;
            var sut = new KeycloakTokenCacheService(settings, code => exitCode = code, httpClient);

            // Act
            await sut.GetOrRefreshTokenAsync();

            // Assert
            Assert.That(exitCode, Is.EqualTo(-1));
        }

        // ─── Scenario 7: Missing credentials returns fallback token ───────────────

        [Test]
        public async Task GetOrRefreshTokenAsync_ShouldReturnFallbackToken_WhenCredentialsMissing()
        {
            // Arrange — no ClientId or ClientSecret
            var settings = new TokenServiceSettings
            {
                SW360BaseUrl = "https://sw360.example.com",
                ClientId = null,
                ClientSecret = null,
                KeyCloakToken = "fallback-static-token",
                KeyCloakTokenType = "Token"
            };
            var sut = new KeycloakTokenCacheService(settings, _ => { });

            // Act
            string token = await sut.GetOrRefreshTokenAsync();

            // Assert — no HTTP call, fallback token returned
            Assert.That(token, Is.EqualTo("fallback-static-token"));
        }

        // ─── Scenario 8: Token expiry causes re-fetch ─────────────────────────────

        [Test]
        public async Task GetOrRefreshTokenAsync_ShouldRefetchToken_WhenTokenExpired()
        {
            // Arrange — token expires in 61 seconds, buffer=60, so effective expiry=1 second
            var settings = BuildSettings();
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(TokenJson(ValidToken, expiresIn: 61)) })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(TokenJson(NewToken, expiresIn: 36000)) });

            var httpClient = new HttpClient(handlerMock.Object);
            var sut = new KeycloakTokenCacheService(settings, _ => { }, httpClient);

            // Act
            string first = await sut.GetOrRefreshTokenAsync();
            await Task.Delay(TimeSpan.FromSeconds(2));                   // wait for expiry (1s effective)
            string second = await sut.GetOrRefreshTokenAsync();

            // Assert
            Assert.That(first, Is.EqualTo(ValidToken));
            Assert.That(second, Is.EqualTo(NewToken));                   // re-fetched after expiry
        }

        // ─── Scenario 9: Empty access token calls exitAction ─────────────────────

        [Test]
        public async Task GetOrRefreshTokenAsync_ShouldCallExitAction_WhenAccessTokenIsEmpty()
        {
            // Arrange
            var settings = BuildSettings();
            var httpClient = BuildMockHttpClient(HttpStatusCode.OK,
                "{\"access_token\":\"\",\"token_type\":\"Bearer\",\"expires_in\":36000}");
            int exitCode = 0;
            var sut = new KeycloakTokenCacheService(settings, code => exitCode = code, httpClient);

            // Act
            await sut.GetOrRefreshTokenAsync();

            // Assert
            Assert.That(exitCode, Is.EqualTo(-1));
        }

        // ─── Scenario 10: InvalidateToken resets expiry to MinValue ──────────────

        [Test]
        public async Task InvalidateToken_ShouldResetCacheState()
        {
            // Arrange
            var settings = BuildSettings();
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(TokenJson(ValidToken)) })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(TokenJson(NewToken)) });

            var httpClient = new HttpClient(handlerMock.Object);
            var sut = new KeycloakTokenCacheService(settings, _ => { }, httpClient);

            await sut.GetOrRefreshTokenAsync();   // prime the cache

            // Act
            sut.ClearOldCacheToken();
            string token = await sut.GetOrRefreshTokenAsync();

            // Assert — second fetch returns new token
            Assert.That(token, Is.EqualTo(NewToken));
        }

        // ─── Scenario 11: All three provided — Keycloak fails (401) → fallback to static token ───

        [Test]
        public async Task GetOrRefreshTokenAsync_ShouldFallbackToStaticToken_WhenKeycloak401AndTokenPresent()
        {
            // Arrange — all three provided: ClientId + ClientSecret + static Token
            var settings = BuildSettingsWithFallback();
            var httpClient = BuildMockHttpClient(HttpStatusCode.Unauthorized,
                "{\"error\":\"invalid_client\",\"error_description\":\"Invalid client credentials\"}");
            int exitCode = 0;
            var sut = new KeycloakTokenCacheService(settings, code => exitCode = code, httpClient);

            // Act
            string token = await sut.GetOrRefreshTokenAsync();

            // Assert — no exit; static fallback token returned
            Assert.That(exitCode, Is.EqualTo(0));
            Assert.That(token, Is.EqualTo(StaticFallbackToken));
        }

        // ─── Scenario 12: All three provided — network error → fallback to static token ──────────

        [Test]
        public async Task GetOrRefreshTokenAsync_ShouldFallbackToStaticToken_WhenNetworkErrorAndTokenPresent()
        {
            // Arrange
            var settings = BuildSettingsWithFallback();
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            var httpClient = new HttpClient(handlerMock.Object);
            int exitCode = 0;
            var sut = new KeycloakTokenCacheService(settings, code => exitCode = code, httpClient);

            // Act
            string token = await sut.GetOrRefreshTokenAsync();

            // Assert — no exit; static fallback token returned
            Assert.That(exitCode, Is.EqualTo(0));
            Assert.That(token, Is.EqualTo(StaticFallbackToken));
        }

        // ─── Scenario 13: All three provided — timeout → fallback to static token ───────────────

        [Test]
        public async Task GetOrRefreshTokenAsync_ShouldFallbackToStaticToken_WhenTimeoutAndTokenPresent()
        {
            // Arrange
            var settings = BuildSettingsWithFallback();
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Request timed out"));

            var httpClient = new HttpClient(handlerMock.Object);
            int exitCode = 0;
            var sut = new KeycloakTokenCacheService(settings, code => exitCode = code, httpClient);

            // Act
            string token = await sut.GetOrRefreshTokenAsync();

            // Assert — no exit; static fallback token returned
            Assert.That(exitCode, Is.EqualTo(0));
            Assert.That(token, Is.EqualTo(StaticFallbackToken));
        }
    }
}
