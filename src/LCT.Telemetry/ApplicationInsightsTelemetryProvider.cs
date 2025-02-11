using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace LCT.Telemetry
{
    public class ApplicationInsightsTelemetryProvider : ITelemetryProvider
    {
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsTelemetryProvider(Dictionary<string, string> configuration)
        {
            var instrumentationKey = configuration.GetValueOrDefault("InstrumentationKey")
                                 ?? Environment.GetEnvironmentVariable("TelemetryInstrumentationKey");

            if (string.IsNullOrEmpty(instrumentationKey))
            {
                throw new InvalidOperationException("Application Insights Instrumentation Key is missing or invalid.");
            }

            var aiConfig = TelemetryConfiguration.CreateDefault();
            aiConfig.InstrumentationKey = instrumentationKey;

            _telemetryClient = new TelemetryClient(aiConfig);
        }

        public void TrackEvent(string eventName, Dictionary<string, string> properties = null)
        {
            _telemetryClient.TrackEvent(eventName, properties);
        }

        public void TrackException(Exception ex, Dictionary<string, string> properties = null)
        {
            var exceptionTelemetry = new ExceptionTelemetry(ex);

            if (properties != null)
            {
                foreach (var property in properties)
                {
                    exceptionTelemetry.Properties[property.Key] = property.Value;
                }
            }

            _telemetryClient.TrackException(exceptionTelemetry);
        }

        public void Flush()
        {
            _telemetryClient.Flush();
            Thread.Sleep(1000); // Allow some time for telemetry to be sent
        }
    }
}