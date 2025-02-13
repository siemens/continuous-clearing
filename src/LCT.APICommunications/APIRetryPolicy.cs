using log4net;
using Polly;
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    public class APIRetryPolicy
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return Policy
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
                        Logger.Debug($"Retry attempt {attempt} due to: {(outcome.Exception != null ? outcome.Exception.Message : $"{outcome.Result.StatusCode} - {outcome.Result.ReasonPhrase}")}");
                    });
        }
        public static IAsyncPolicy GetWebExceptionRetryPolicy()
        {
            return Policy
                .Handle<WebException>()
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
                        Logger.Debug($"Retry attempt {attempt} due to: {outcome.Message}. Waiting {timespan} before next retry.");
                    });
        }
    }
}
