// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using log4net;
using Polly;
using SIT.Common;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.APICommunications
{
    /// <summary>
    /// A delegating handler that implements retry logic for HTTP requests using Polly policies.
    /// Handles transient failures (5xx, 408, 406, 400) and network exceptions with configurable wait intervals.
    /// Token refresh on 401 Unauthorized is handled upstream by <see cref="SW360KeycloakService.TokenRefreshDelegatingHandler"/>.
    /// </summary>
    public class RetryHttpClientHandler : DelegatingHandler
    {
        #region Fields

        /// <summary>
        /// The asynchronous retry policy for handling transient HTTP failures.
        /// </summary>
        private readonly AsyncPolicy<HttpResponseMessage> _retryPolicy;

        /// <summary>
        /// The logger instance for logging retry attempts and related information.
        /// </summary>
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Flag indicating whether the initial retry has been logged.
        /// </summary>
        private bool _initialRetryLogged = false;

        /// <summary>
        /// The timeout applied to each individual HTTP attempt. When this elapses the current attempt is
        /// cancelled and (if retries remain) retried, instead of the whole retry chain sharing a single
        /// <see cref="HttpClient.Timeout"/> budget.
        /// </summary>
        private readonly TimeSpan _perAttemptTimeout;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryHttpClientHandler"/> class with a default retry policy.
        /// </summary>
        /// <param name="perAttemptTimeoutSeconds">
        /// The timeout, in seconds, applied to each individual HTTP attempt. Use a value less than or equal to zero
        /// to disable the per-attempt timeout.
        /// </param>
        public RetryHttpClientHandler(int perAttemptTimeoutSeconds = 0)
        {
            _perAttemptTimeout = perAttemptTimeoutSeconds > 0
                ? TimeSpan.FromSeconds(perAttemptTimeoutSeconds)
                : Timeout.InfiniteTimeSpan;

            // Define the retry policy (retry on 5xx, 408, 406, 400 and transient errors; exclude 401 and 403)
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .OrResult<HttpResponseMessage>(r =>
                    (r.StatusCode == HttpStatusCode.RequestTimeout
                    || r.StatusCode == HttpStatusCode.NotAcceptable
                    || r.StatusCode == HttpStatusCode.BadRequest
                    || (int)r.StatusCode >= 500)
                    && r.StatusCode != HttpStatusCode.Unauthorized
                    && r.StatusCode != HttpStatusCode.Forbidden)
                .WaitAndRetryAsync(ApiConstant.APIRetryIntervals.Count,
                    GetRetryInterval,
                    OnRetry);
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Handles the retry callback when a retry attempt occurs.
        /// </summary>
        /// <param name="outcome">The result of the failed request attempt.</param>
        /// <param name="timespan">The time span to wait before the next retry.</param>
        /// <param name="attempt">The current retry attempt number.</param>
        /// <param name="context">The context containing request metadata.</param>
        private void OnRetry(DelegateResult<HttpResponseMessage> outcome, TimeSpan timespan, int attempt, Context context)
        {
            var httpMethod = context.ContainsKey("HttpMethod") ? context["HttpMethod"] : "Unknown Method";
            var requestUri = context.ContainsKey("RequestUri") ? context["RequestUri"] : "Unknown URI";
            var operationInfo = context.ContainsKey("OperationInfo") ? context["OperationInfo"] : requestUri;


            Logger.DebugFormat("Retry attempt {0} for {1} method this URL {2} : {3}", attempt, httpMethod, requestUri, (outcome.Exception != null ? outcome.Exception.Message : outcome.Result.StatusCode.ToString()));

            if (!_initialRetryLogged && context["LogWarnings"] as bool? != false)
            {
                Logger.WarnFormat("Retry attempt initiated: {0} Error: {1}", operationInfo, (outcome.Exception != null ? outcome.Exception.Message : outcome.Result.StatusCode.ToString()));
            }

            context["RetryAttempt"] = attempt;
            _initialRetryLogged = true;
        }

        /// <summary>
        /// Asynchronously sends an HTTP request with retry logic applied.
        /// When a 401 Unauthorized response is received and a token service is configured,
        /// the cached token is invalidated, a new token is fetched, and the request is retried once.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>An HttpResponseMessage representing the response from the server.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var context = new Context
            {
                ["LogWarnings"] = request.Headers.TryGetValues("LogWarnings", out var logWarningsValues) && bool.TryParse(logWarningsValues.FirstOrDefault(), out var logWarnings) && logWarnings,
                ["HttpMethod"] = request.Method.ToString(),
                ["RequestUri"] = request.RequestUri?.ToString(),
                ["OperationInfo"] = request.Headers.TryGetValues("urlInfo", out var operationInfoValues) ? operationInfoValues.FirstOrDefault() : ""
            };

            var response = await _retryPolicy.ExecuteAsync(async (ctx) =>
            {
                // Apply a per-attempt timeout so a single slow attempt is cancelled and retried, rather than
                // every attempt (plus back-off waits) sharing one HttpClient.Timeout budget that, once exceeded,
                // cancels the whole operation.
                if (_perAttemptTimeout == Timeout.InfiniteTimeSpan)
                {
                    return await base.SendAsync(request, cancellationToken); // Pass the request to the next handler (HttpClient)
                }

                using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                attemptCts.CancelAfter(_perAttemptTimeout);
                try
                {
                    return await base.SendAsync(request, attemptCts.Token);
                }
                catch (OperationCanceledException) when (attemptCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    // The per-attempt timeout elapsed (not an external cancellation); surface as a timeout so the
                    // retry policy treats it as a transient failure and retries.
                    throw new TaskCanceledException($"The attempt timed out after {_perAttemptTimeout.TotalSeconds} seconds.");
                }
            }, context);

            if (_initialRetryLogged)
            {
                var attempt = context.ContainsKey("RetryAttempt") ? context["RetryAttempt"] : 0;
                Logger.DebugFormat("Retry attempt successful after {0} attempts for {1} {2}.", attempt, request.Method, request.RequestUri);
                _initialRetryLogged = false;
            }

            return response;
        }

        /// <summary>
        /// Asynchronously executes an action with retry logic for handling WebException failures.
        /// </summary>
        /// <param name="action">The asynchronous action to execute with retry support.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public static async Task ExecuteWithRetryAsync(Func<Task> action)
        {
            var retryPolicy = Policy
                .Handle<WebException>()
                .WaitAndRetryAsync(ApiConstant.APIRetryIntervals.Count,
                    GetRetryInterval,
                    onRetry: (exception, timespan, attempt, context) =>
                    {
                        Logger.DebugFormat("Retry attempt {0} due to: {1}", attempt, (exception?.Message ?? "No exception"));
                    });

            await retryPolicy.ExecuteAsync(action);
        }

        /// <summary>
        /// Gets the retry interval for a given attempt number.
        /// </summary>
        /// <param name="attempt">The current retry attempt number.</param>
        /// <returns>A TimeSpan representing the wait duration before the next retry.</returns>
        private static TimeSpan GetRetryInterval(int attempt)
        {
            if (attempt >= 1 && attempt <= ApiConstant.APIRetryIntervals.Count)
                return TimeSpan.FromSeconds(ApiConstant.APIRetryIntervals[attempt - 1]);
            return TimeSpan.Zero; // Default if out of range
        }

        #endregion Methods
    }
}
