using log4net;
using Polly;
using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    public class RetryWebClientHandler
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);        
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
