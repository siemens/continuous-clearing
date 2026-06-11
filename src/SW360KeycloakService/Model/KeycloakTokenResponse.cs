// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;

namespace SW360KeycloakService.Model
{
    /// <summary>
    /// Represents the token response returned by Keycloak after a successful client credentials grant.
    /// </summary>
    public sealed class KeycloakTokenResponse
    {
        /// <summary>
        /// Gets or sets the access token issued by Keycloak.
        /// </summary>
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the token type (e.g., Bearer).
        /// </summary>
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        /// <summary>
        /// Gets or sets the lifetime in seconds of the access token.
        /// </summary>
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
