// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

namespace SW360KeycloakService.Model
{
    /// <summary>
    /// Holds the connection settings required to fetch a Keycloak token.
    /// Populated from <c>CommonAppSettings.SW360</c> by the caller.
    /// </summary>
    public class TokenServiceSettings
    {
        /// <summary>
        /// Gets or sets the SW360 base URL used to build the Keycloak token endpoint.
        /// </summary>
        public string SW360BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the Keycloak client identifier.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the Keycloak client secret.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the existing static token used as fallback when Keycloak credentials are absent.
        /// </summary>
        public string KeyCloakToken { get; set; }

        /// <summary>
        /// Gets or sets the token type (e.g., Bearer). Updated after a successful token fetch.
        /// </summary>
        public string KeyCloakTokenType { get; set; }
    }
}
