// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

namespace SW360KeycloakService.Constants
{
    /// <summary>
    /// Constants used by the Keycloak token service.
    /// </summary>
    internal static class KeycloakConstants
    {
        /// <summary>Keycloak token endpoint path relative to the SW360 base URL.</summary>
        internal const string TokenEndpoint = "/kc/realms/sw360/protocol/openid-connect/token";

        /// <summary>Keycloak form field key: grant_type.</summary>
        internal const string GrantTypeKey = "grant_type";

        /// <summary>Keycloak grant type value for client credentials flow.</summary>
        internal const string GrantTypeValue = "client_credentials";

        /// <summary>Keycloak form field key: client_id.</summary>
        internal const string ClientIdKey = "client_id";

        /// <summary>Keycloak form field key: client_secret.</summary>
        internal const string ClientSecretKey = "client_secret";

        /// <summary>Keycloak form field key: scope.</summary>
        internal const string ScopeKey = "scope";

        /// <summary>Keycloak scope value used during token request.</summary>
        internal const string ScopeValue = "openid email READ WRITE";

        /// <summary>Safety buffer in seconds subtracted from ExpiresIn when caching.</summary>
        internal const int ExpiryBufferSeconds = 60;
    }
}
