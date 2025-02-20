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
                .Handle<HttpRequestException>() 
                .Or<TaskCanceledException>()    
                .OrResult<HttpResponseMessage>(r =>
                    (r.StatusCode == HttpStatusCode.RequestTimeout
                    || (int)r.StatusCode >= 500) 
                    && r.StatusCode != HttpStatusCode.Unauthorized
                    && r.StatusCode != HttpStatusCode.Forbidden)
                .WaitAndRetryAsync(ApiConstant.APIRetryCount,
                     GetRetryInterval,
                    onRetry: (outcome, timespan, attempt, context) =>
                    {
                        Logger.Debug($"Retry attempt {attempt} due to: {(outcome.Exception != null ? outcome.Exception.Message : $"{outcome.Result.StatusCode}")}");
                        Logger.Warn($"Retry attempt {attempt} will be triggered in {timespan.TotalSeconds} seconds due to: {(outcome.Exception != null ? outcome.Exception.Message : $"{outcome.Result.StatusCode}")}");
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
                .WaitAndRetryAsync(ApiConstant.APIRetryCount,
                    GetRetryInterval,
                    onRetry: (exception, timespan, attempt, context) =>
                    {
                        Logger.Debug($"Retry attempt {attempt} due to: {exception?.Message ?? "No exception"}");                        
                    });

            await retryPolicy.ExecuteAsync(action);
        }
        private static TimeSpan GetRetryInterval(int attempt)
        {
            // Define retry intervals as constants or values
            var retryIntervals = new[] { ApiConstant.APIRetryIntervalFirst, ApiConstant.APIRetryIntervalSecond, ApiConstant.APIRetryIntervalThird }; // Retry intervals for 1st, 2nd, and 3rd attempts
            if (attempt >= 1 && attempt <= retryIntervals.Length)
                return TimeSpan.FromSeconds(retryIntervals[attempt - 1]);
            return TimeSpan.Zero; // Default if out of range
        }
    }
}
