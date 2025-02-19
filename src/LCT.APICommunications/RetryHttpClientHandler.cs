using log4net;
using Polly;
using System;
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
        public RetryHttpClientHandler()
        {
            // Define the retry policy (retry on 5xx, 408, and transient errors)
            _retryPolicy = Policy
                .Handle<HttpRequestException>()   // Handle HttpRequestException
                .Or<TaskCanceledException>()     // Handle TaskCanceledException (timeouts)
                .OrResult<HttpResponseMessage>(r =>
                    (r.StatusCode == HttpStatusCode.RequestTimeout // 408 Request Timeout
                    || (int)r.StatusCode >= 500) // 5xx server errors
                    && r.StatusCode != HttpStatusCode.Unauthorized // 401 Unauthorized
                    && r.StatusCode != HttpStatusCode.Forbidden) // 403 Forbidden
                .WaitAndRetryAsync(ApiConstant.APIRetryCount, // Retry up to 3 times
                    attempt => attempt switch
                    {
                        1 => TimeSpan.FromSeconds(ApiConstant.APIRetryIntervalFirst),  // 1st retry after 5 seconds
                        2 => TimeSpan.FromSeconds(ApiConstant.APIRetryIntervalSecond), // 2nd retry after 10 seconds
                        3 => TimeSpan.FromSeconds(ApiConstant.APIRetryIntervalThird), // 3rd retry after 30 seconds
                        _ => TimeSpan.Zero // Default (not used)
                    },
                    onRetry: (outcome, timespan, attempt, context) =>
                    {
                        Logger.Debug($"Retry attempt {attempt} due to: {(outcome.Exception != null ? outcome.Exception.Message : $"{outcome.Result.StatusCode}")}");
                    });
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await base.SendAsync(request, cancellationToken); // Pass the request to the next handler (HttpClient)
            });
        }
        public static async Task ExecuteWithRetryAsync(Func<Task> action)
        {
            var retryPolicy = Policy
                .Handle<WebException>()
                .WaitAndRetryAsync(ApiConstant.APIRetryCount, // Retry up to 3 times
                    attempt => attempt switch
                    {
                        1 => TimeSpan.FromSeconds(ApiConstant.APIRetryIntervalFirst),  // 1st retry after 5 seconds
                        2 => TimeSpan.FromSeconds(ApiConstant.APIRetryIntervalSecond), // 2nd retry after 10 seconds
                        3 => TimeSpan.FromSeconds(ApiConstant.APIRetryIntervalThird), // 3rd retry after 30 seconds
                        _ => TimeSpan.Zero // Default (not used)
                    },
                    onRetry: (exception, timespan, attempt, context) =>
                    {
                        Logger.Debug($"Retry attempt {attempt} due to: {exception?.Message ?? "No exception"}");
                    });

            await retryPolicy.ExecuteAsync(action);
        }
    }
}
