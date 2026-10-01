// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using Telemetry.Uploader;

namespace SIT.Telemetry
{
    /// <summary>
    /// Provides telemetry tracking functionality using the shared Telemetry.Uploader SDK (Application Insights backed).
    /// </summary>
    public class ApplicationInsightsTelemetryProvider : ITelemetryProvider
    {
        #region Fields
        private readonly ITelemetryService _telemetryService;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsTelemetryProvider"/> class.
        /// </summary>
        /// <param name="configuration">The configuration dictionary containing connection string and app identity information.</param>
        /// <exception cref="InvalidOperationException">Thrown when the Application Insights Instrumentation Key is missing or invalid.</exception>
        public ApplicationInsightsTelemetryProvider(Dictionary<string, string> configuration)
        {
            var connectionString = configuration.GetValueOrDefault("ConnectionString")
                                 ?? Environment.GetEnvironmentVariable("TelemetryConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Application Insights Instrumentation Key is missing or invalid.");
            }

            var options = new TelemetryOptions
            {
                ConnectionString = connectionString,
                Product = configuration.GetValueOrDefault("Product") ?? "SBOM",
                Application = configuration.GetValueOrDefault("Application") ?? "ContinuousClearing",
                Component = configuration.GetValueOrDefault("Component") ?? "CLI",
                Environment = configuration.GetValueOrDefault("Environment") ?? "Production",
                Version = configuration.GetValueOrDefault("Version") ?? string.Empty,
                Organization = configuration.GetValueOrDefault("Organization") ?? "Siemens"
            };

            _telemetryService = new ApplicationInsightsTelemetryService(options);
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
            _telemetryService.TrackEvent(eventName, properties);
        }

        /// <summary>
        /// Tracks an exception with optional properties.
        /// </summary>
        /// <param name="ex">The exception to track.</param>
        /// <param name="properties">Optional dictionary of properties to include with the exception.</param>
        public void TrackException(Exception ex, Dictionary<string, string>? properties = null)
        {
            _telemetryService.TrackException(ex, properties);
        }

        /// <summary>
        /// Tracks a numeric metric value with optional dimensions/properties.
        /// </summary>
        /// <remarks>
        /// The Telemetry.Uploader SDK is event-schema based and has no dedicated metric API, so the
        /// value is emitted as a custom event with the value included as a property.
        /// </remarks>
        /// <param name="metricName">The name of the metric to track.</param>
        /// <param name="value">The numeric value of the metric.</param>
        /// <param name="properties">Optional dictionary of dimensions to include with the metric.</param>
        public void TrackMetric(string metricName, double value, Dictionary<string, string>? properties = null)
        {
            var metricProperties = properties != null
                ? new Dictionary<string, string>(properties)
                : new Dictionary<string, string>();

            metricProperties["MetricValue"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            _telemetryService.TrackEvent(metricName, metricProperties);
        }

        /// <summary>
        /// Flushes the telemetry service to ensure all telemetry data is sent.
        /// </summary>
        public void Flush()
        {
            _telemetryService.Flush();
        }
        #endregion
    }
}