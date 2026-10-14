// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using SW360KeycloakService;
using SW360KeycloakService.Model;
using System.Diagnostics.CodeAnalysis;

namespace TestUtilities
{
    /// <summary>
    /// Fetches a Keycloak access token once (using ClientId/ClientSecret) at the start of the
    /// integration test run and caches it for the lifetime of the test process. The token is
    /// typically valid for several hours, which is enough time to run the full test suite, so
    /// individual test methods can reuse the cached value instead of authenticating themselves.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class TestAuthHelper
    {
        private static readonly object s_lock = new object();
        private static bool s_initialized;
        private static string s_tokenType;
        private static string s_tokenValue;

        /// <summary>
        /// Gets the token type (e.g. "Bearer") for the cached Keycloak token.
        /// </summary>
        public static string TokenType
        {
            get
            {
                EnsureInitialized();
                return s_tokenType;
            }
        }

        /// <summary>
        /// Gets the cached Keycloak access token value.
        /// </summary>
        public static string TokenValue
        {
            get
            {
                EnsureInitialized();
                return s_tokenValue;
            }
        }

        private static void EnsureInitialized()
        {
            if (s_initialized)
            {
                return;
            }

            lock (s_lock)
            {
                if (s_initialized)
                {
                    return;
                }

                var testParam = new TestParam();

                if (string.IsNullOrWhiteSpace(testParam.KeycloakClientId) ||
                    string.IsNullOrWhiteSpace(testParam.KeycloakClientSecret))
                {
                    throw new InvalidOperationException(
                        "Integration tests require Keycloak credentials. 'KeycloakClientId' and " +
                        "'KeycloakClientSecret' are missing from appSettingsSW360IntegrationTest.json. " +
                        "Ensure the pipeline variables SW360_CLIENT_ID and SW360_CLIENT_SECRET are set in " +
                        "the variable group and written into the test config by the 'Update Integration " +
                        "Test Configuration' step.");
                }

                var settings = new TokenServiceSettings
                {
                    SW360BaseUrl = testParam.SW360URL,
                    ClientId = testParam.KeycloakClientId,
                    ClientSecret = testParam.KeycloakClientSecret
                };

                using var tokenService = new KeycloakTokenCacheService(settings);
                s_tokenValue = tokenService.GetOrRefreshTokenAsync().GetAwaiter().GetResult();
                s_tokenType = string.IsNullOrWhiteSpace(settings.KeyCloakTokenType)
                    ? "Bearer"
                    : settings.KeyCloakTokenType;

                s_initialized = true;
            }
        }
    }
}
