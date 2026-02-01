// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace LCT.Telemetry
{
    /// <summary>
    /// Provides telemetry tracking functionality using Application Insights.
    /// </summary>
    public class ApplicationInsightsTelemetryProvider : ITelemetryProvider
    {
        #region Fields
        private readonly TelemetryClient _telemetryClient;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsTelemetryProvider"/> class.
        /// </summary>
        /// <param name="configuration">The configuration dictionary containing connection string.</param>
        /// <exception cref="InvalidOperationException">Thrown when the Application Insights Instrumentation Key is missing or invalid.</exception>
        public ApplicationInsightsTelemetryProvider(Dictionary<string, string> configuration)
        {
            var ConnectionString = configuration.GetValueOrDefault("ConnectionString")
                                 ?? Environment.GetEnvironmentVariable("TelemetryConnectionString");

            if (string.IsNullOrEmpty(ConnectionString))
            {
                throw new InvalidOperationException("Application Insights Instrumentation Key is missing or invalid.");
            }

            var aiConfig = TelemetryConfiguration.CreateDefault();
            aiConfig.ConnectionString = $"InstrumentationKey={ConnectionString}";

            _telemetryClient = new TelemetryClient(aiConfig);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Tracks a custom event with optional properties.
        /// </summary>
        /// <param name="eventName">The name of the event to track.</param>
        /// <param name="properties">Optional dictionary of properties to include with the event.</param>
        public void TrackEvent(string eventName, Dictionary<string, string>? properties = null)
        {
            _telemetryClient.TrackEvent(eventName, properties);
        }

        /// <summary>
        /// Tracks an exception with optional properties.
        /// </summary>
        /// <param name="ex">The exception to track.</param>
        /// <param name="properties">Optional dictionary of properties to include with the exception.</param>
        public void TrackException(Exception ex, Dictionary<string, string>? properties = null)
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

        /// <summary>
        /// Flushes the telemetry client to ensure all telemetry data is sent.
        /// </summary>
        public void Flush()
        {
            _telemetryClient.Flush();
            Thread.Sleep(1000); // Allow some time for telemetry to be sent
        }
        #endregion
    }
}