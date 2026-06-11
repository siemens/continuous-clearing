// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using log4net;
using Newtonsoft.Json;
using SW360KeycloakService.Constants;
using SW360KeycloakService.Interfaces;
using SW360KeycloakService.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SW360KeycloakService
{
    /// <summary>
    /// Caches a Keycloak access token in memory and refreshes it automatically when it expires.
    /// Thread-safe via <see cref="SemaphoreSlim"/> double-check locking.
    /// </summary>
    public class KeycloakTokenCacheService : IKeycloakTokenService
    {
        #region Fields

        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly TokenServiceSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly Action<int> _exitAction;

        private string _cachedAccessToken;
        private DateTimeOffset _accessTokenExpiresAt = DateTimeOffset.MinValue;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of <see cref="KeycloakTokenCacheService"/>.
        /// </summary>
        /// <param name="settings">Keycloak connection settings.</param>
        /// <param name="exitAction">
        /// Action invoked on fatal error to exit the application (e.g., <c>Environment.Exit</c>).
        /// Defaults to <c>Environment.Exit</c> when not provided.
        /// </param>
        /// <param name="httpClient">Optional <see cref="HttpClient"/> for token endpoint requests. A new one is created if not provided.</param>
        public KeycloakTokenCacheService(
            TokenServiceSettings settings,
            Action<int> exitAction = null,
            HttpClient httpClient = null)
        {
            _settings = settings;
            _exitAction = exitAction ?? (code => Environment.Exit(code));
            _httpClient = httpClient ?? new HttpClient();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a valid access token from cache, or fetches a new one if missing or expired.
        /// Uses double-check locking with <see cref="SemaphoreSlim"/> to prevent concurrent fetch races.
        /// </summary>
        /// <returns>A valid Bearer access token string, or <c>null</c> if credentials are not configured.</returns>
        public async Task<string> GetOrRefreshTokenAsync()
        {
            if (_cachedAccessToken != null && DateTimeOffset.UtcNow < _accessTokenExpiresAt)
            {
                Logger.Debug("KeycloakTokenCacheService: Returning cached access token.");
                return _cachedAccessToken;
            }

            await _lock.WaitAsync();
            try
            {
                // Double-check after acquiring the lock
                if (_cachedAccessToken != null && DateTimeOffset.UtcNow < _accessTokenExpiresAt)
                {
                    Logger.Debug("KeycloakTokenCacheService: Returning cached access token (post-lock check).");
                    return _cachedAccessToken;
                }

                return await GenerateAccessTokenAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Removes the cached token so the next call to <see cref="GetOrRefreshTokenAsync"/> forces a fresh fetch.
        /// Call this when a 401 Unauthorized response is received.
        /// </summary>
        public void ClearOldCacheToken()
        {
            _cachedAccessToken = null;
            _accessTokenExpiresAt = DateTimeOffset.MinValue;
            Logger.Debug("KeycloakTokenCacheService: Cleared last keycloak token.");
        }

        #endregion

        #region Private Methods

        private bool HasStaticTokenFallback => !string.IsNullOrWhiteSpace(_settings?.KeyCloakToken);

        /// <summary>
        /// Returns the static fallback token when one is configured, logging a warning.
        /// Otherwise logs the user-facing error, calls the exit action, and returns <c>null</c>.
        /// </summary>
        private string FallbackToOldTokenOrExit(string userFacingErrorMessage)
        {
            if (HasStaticTokenFallback)
            {
                Logger.Debug("KeycloakTokenCacheService: Keycloak authentication failed; falling back to static token.");                
                return _settings.KeyCloakToken;
            }
            Logger.Error(userFacingErrorMessage);
            _exitAction(-1);
            return null;
        }

        private async Task<string> GenerateAccessTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(_settings?.ClientId) ||
                string.IsNullOrWhiteSpace(_settings?.ClientSecret) ||
                string.IsNullOrWhiteSpace(_settings?.SW360BaseUrl))
            {
                Logger.Debug("KeycloakTokenCacheService: ClientId, ClientSecret or SW360 URL not set. Falling back to existing token.");
                return _settings?.KeyCloakToken;
            }

            string tokenUrl = $"{_settings.SW360BaseUrl.TrimEnd('/')}{KeycloakConstants.TokenEndpoint}";

            Logger.Debug("KeycloakTokenCacheService: Fetching new Keycloak access token.");

            HttpResponseMessage response;
            try
            {
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>(KeycloakConstants.GrantTypeKey,    KeycloakConstants.GrantTypeValue),
                    new KeyValuePair<string, string>(KeycloakConstants.ClientIdKey,     _settings.ClientId),
                    new KeyValuePair<string, string>(KeycloakConstants.ClientSecretKey, _settings.ClientSecret),
                    new KeyValuePair<string, string>(KeycloakConstants.ScopeKey,        KeycloakConstants.ScopeValue)
                });
                response = await _httpClient.PostAsync(tokenUrl, formContent);
            }
            catch (HttpRequestException ex)
            {
                Logger.DebugFormat("KeycloakTokenCacheService: HTTP error fetching token from {0}. Error: {1}", tokenUrl, ex.Message);
                return FallbackToOldTokenOrExit("Keycloak token generation failed. Unable to connect to the Sw360 server.");
            }
            catch (TaskCanceledException ex)
            {
                Logger.DebugFormat("KeycloakTokenCacheService: Token request timed out for {0}. Error: {1}", tokenUrl, ex.Message);
                return FallbackToOldTokenOrExit("Keycloak token request timed out. The Sw360 server did not respond in time. Please check network connectivity or try again later.");
            }

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                Logger.DebugFormat("KeycloakTokenCacheService: Keycloak token request failed. Status: {0}, Body: {1}", response.StatusCode, errorBody);
                return FallbackToOldTokenOrExit($"Keycloak Token generation failed. Please verify your ClientId and ClientSecret via inline or appSettings.json file and retry again. (Status: {response.StatusCode})");
            }

            string responseBody = await response.Content.ReadAsStringAsync();
            KeycloakTokenResponse tokenResponse;
            try
            {
                tokenResponse = JsonConvert.DeserializeObject<KeycloakTokenResponse>(responseBody);
            }
            catch (JsonException ex)
            {
                Logger.DebugFormat("KeycloakTokenCacheService: Failed to deserialize token response. Error: {0}", ex.Message);
                return FallbackToOldTokenOrExit("Keycloak token response could not be parsed. An unexpected response was received from the Keycloak server.");
            }

            if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
            {
                Logger.Debug("KeycloakTokenCacheService: Received empty access token from Keycloak.");
                return FallbackToOldTokenOrExit("Keycloak returned an empty access token.");
            }

            // Use ExpiresIn from the response minus a safety buffer to avoid edge-expiry 401s
            int expirySeconds = tokenResponse.ExpiresIn > KeycloakConstants.ExpiryBufferSeconds
                ? tokenResponse.ExpiresIn - KeycloakConstants.ExpiryBufferSeconds
                : tokenResponse.ExpiresIn;

            _cachedAccessToken = tokenResponse.AccessToken;
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expirySeconds);

            // Keep settings in sync so callers reading them directly get the fresh values
            _settings.KeyCloakToken = tokenResponse.AccessToken;
            _settings.KeyCloakTokenType = tokenResponse.TokenType;

            Logger.DebugFormat("KeycloakTokenCacheService: Token cached for {0} seconds.", expirySeconds);
            return tokenResponse.AccessToken;
        }        

        #endregion
    }
}
