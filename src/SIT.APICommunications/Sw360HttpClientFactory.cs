// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using SIT.Common.Model;
using SW360KeycloakService;
using SW360KeycloakService.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;

namespace SIT.APICommunications
{
    /// <summary>
    /// Builds and caches <see cref="IHttpClientFactory"/> instances for SW360 API communication so that
    /// HttpClient/handler instances are pooled and reused (via IHttpClientFactory's handler rotation)
    /// instead of a brand-new HttpClientHandler being created on every single API call.
    /// </summary>
    internal static class Sw360HttpClientFactory
    {
        private const string ClientName = "Sw360Client";
        private static readonly ConcurrentDictionary<string, IHttpClientFactory> Factories = new();

        /// <summary>
        /// Gets (creating if necessary) a pooled HttpClient configured for the given SW360 connection settings.
        /// </summary>
        public static HttpClient CreateClient(SW360ConnectionSettings sw360ConnectionSettings, IKeycloakTokenService tokenService, int perAttemptTimeoutSeconds)
        {
            // One factory per distinct SW360 URL/token-service combination is enough; settings are effectively
            // static for the lifetime of a pipeline run.
            string cacheKey = $"{sw360ConnectionSettings.SW360URL}|{tokenService != null}";
            IHttpClientFactory factory = Factories.GetOrAdd(cacheKey, _ => BuildFactory(sw360ConnectionSettings, tokenService, perAttemptTimeoutSeconds));
            return factory.CreateClient(ClientName);
        }

        private static IHttpClientFactory BuildFactory(SW360ConnectionSettings sw360ConnectionSettings, IKeycloakTokenService tokenService, int perAttemptTimeoutSeconds)
        {
            var services = new ServiceCollection();
            IHttpClientBuilder builder = services.AddHttpClient(ClientName, client =>
            {
                // Per-attempt timeout is enforced inside RetryHttpClientHandler, so the chain of retries isn't
                // capped by a single HttpClient-level timeout.
                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ApiConstant.ApplicationJson));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ApiConstant.ApplicationHalJson));
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(sw360ConnectionSettings.SW360AuthTokenType, sw360ConnectionSettings.Sw360Token);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
            .AddHttpMessageHandler(() => new RetryHttpClientHandler(perAttemptTimeoutSeconds));

            if (tokenService != null)
            {
                // Mutates the outgoing request's Authorization header on 401 retry, so it stays correct even
                // though the underlying HttpClient/handler chain is now long-lived and pooled.
                builder.AddHttpMessageHandler(() => new TokenRefreshDelegatingHandler(tokenService));
            }

            return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        }
    }
}
