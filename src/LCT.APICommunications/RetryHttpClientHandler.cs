// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using LCT.Common;
using log4net;
using Polly;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    /// <summary>
    /// A delegating handler that implements retry logic for HTTP requests using Polly policies.
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

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryHttpClientHandler"/> class with a default retry policy.
        /// </summary>
        public RetryHttpClientHandler()
        {
            // Define the retry policy (retry on 5xx, 408, and transient errors)
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


            Logger.Debug($"Retry attempt {attempt} for {httpMethod} method this URL {requestUri} : {(outcome.Exception != null ? outcome.Exception.Message : $"{outcome.Result.StatusCode}")}");

            if (!_initialRetryLogged && context["LogWarnings"] as bool? != false)
            {
                string errorMsg = outcome.Exception != null ? outcome.Exception.Message : (outcome.Result != null ? outcome.Result.StatusCode.ToString() : "");
                Logger.WarnFormat("Retry attempt initiated: {0} Error: {1}", operationInfo, errorMsg);
            }

            context["RetryAttempt"] = attempt;
            _initialRetryLogged = true;
        }

        /// <summary>
        /// Asynchronously sends an HTTP request with retry logic applied.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>An HttpResponseMessage representing the response from the server.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var context = new Context
            {
                ["LogWarnings"] = request.Headers.TryGetValues("LogWarnings", out var logWarningsValues) && bool.TryParse(logWarningsValues.FirstOrDefault(), out var logWarnings) ? logWarnings : default,
                ["HttpMethod"] = request.Method.ToString(),
                ["RequestUri"] = request.RequestUri?.ToString(),
                ["OperationInfo"] = request.Headers.TryGetValues("urlInfo", out var operationInfoValues) ? operationInfoValues.FirstOrDefault() : ""
            };

            var response = await _retryPolicy.ExecuteAsync(async (ctx) =>
            {
                return await base.SendAsync(request, cancellationToken); // Pass the request to the next handler (HttpClient)
            }, context);

            if (_initialRetryLogged)
            {
                var attempt = context.ContainsKey("RetryAttempt") ? context["RetryAttempt"] : 0;
                Logger.Debug($"Retry attempt successful after {attempt} attempts for {request.Method} {request.RequestUri}.");
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
                        Logger.Debug($"Retry attempt {attempt} due to: {exception?.Message ?? "No exception"}");
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
