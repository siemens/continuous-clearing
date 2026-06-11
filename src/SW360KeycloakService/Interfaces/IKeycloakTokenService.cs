// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace SW360KeycloakService.Interfaces
{
    /// <summary>
    /// Defines the contract for a Keycloak token cache service.
    /// </summary>
    public interface IKeycloakTokenService
    {
        /// <summary>
        /// Returns a valid access token from cache, or fetches a new one from Keycloak if the cached token
        /// is missing or expired.
        /// </summary>
        /// <returns>A valid Bearer access token string, or <c>null</c> if credentials are not configured.</returns>
        Task<string> GetOrRefreshTokenAsync();

        /// <summary>
        /// Removes the cached token so the next call to <see cref="GetOrRefreshTokenAsync"/> forces a fresh fetch.
        /// Intended to be called when a 401 Unauthorized response is received.
        /// </summary>
        void ClearOldCacheToken();
    }
}
