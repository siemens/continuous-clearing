// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using log4net;
using SW360KeycloakService.Interfaces;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SW360KeycloakService
{
    /// <summary>
    /// A delegating handler that intercepts 401 Unauthorized responses, invalidates the cached
    /// Keycloak token, fetches a fresh one via <see cref="IKeycloakTokenService"/>, and retries
    /// the request once with the new token.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="TokenRefreshDelegatingHandler"/>.
    /// </remarks>
    /// <param name="tokenService">The Keycloak token service used to refresh the access token on 401.</param>
    public class TokenRefreshDelegatingHandler(IKeycloakTokenService tokenService) : DelegatingHandler
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IKeycloakTokenService _tokenService = tokenService;

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            Logger.DebugFormat("TokenRefreshDelegatingHandler: 401 Unauthorized for {0} {1}. Refreshing token and retrying.", request.Method, request.RequestUri);

            _tokenService.ClearOldCacheToken();
            string newToken = await _tokenService.GetOrRefreshTokenAsync();

            if (string.IsNullOrWhiteSpace(newToken))
            {
                Logger.DebugFormat("TokenRefreshDelegatingHandler: Token refresh returned empty token. Returning original 401 for {0} {1}.", request.Method, request.RequestUri);
                return response;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
            response = await base.SendAsync(request, cancellationToken);

            Logger.DebugFormat("TokenRefreshDelegatingHandler: Token refresh retry completed with status {0} for {1} {2}.", response.StatusCode, request.Method, request.RequestUri);
            return response;
        }
    }
}
