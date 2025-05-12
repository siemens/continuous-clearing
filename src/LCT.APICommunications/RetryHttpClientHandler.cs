// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
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
    public class RetryHttpClientHandler : DelegatingHandler
    {
        private readonly AsyncPolicy<HttpResponseMessage> _retryPolicy;
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private bool _initialRetryLogged = false;
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

        private void OnRetry(DelegateResult<HttpResponseMessage> outcome, TimeSpan timespan, int attempt, Context context)
        {
            var httpMethod = context.ContainsKey("HttpMethod") ? context["HttpMethod"] : "Unknown Method";
            var requestUri = context.ContainsKey("RequestUri") ? context["RequestUri"] : "Unknown URI";
            var operationInfo = context.ContainsKey("OperationInfo") ? context["OperationInfo"] : requestUri;
            

            Logger.Debug($"Retry attempt {attempt} for {httpMethod} method this URL {requestUri} : {(outcome.Exception != null ? outcome.Exception.Message : $"{outcome.Result.StatusCode}")}");

            if (!_initialRetryLogged && context["LogWarnings"] as bool? != false)
            {
                Logger.Warn($"Retry attempt triggered for {operationInfo}: {(outcome.Exception != null ? outcome.Exception.Message : $"{outcome.Result.StatusCode}")}");
            }

            context["RetryAttempt"] = attempt;
            _initialRetryLogged = true;
        }
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
        private static TimeSpan GetRetryInterval(int attempt)
        {
            if (attempt >= 1 && attempt <= ApiConstant.APIRetryIntervals.Count)
                return TimeSpan.FromSeconds(ApiConstant.APIRetryIntervals[attempt - 1]);
            return TimeSpan.Zero; // Default if out of range
        }

    }
}
