using System;
using System.Collections.Generic;

namespace LCT.Telemetry
{
    /// <summary>
    /// Enum to define the available telemetry types.
    /// </summary>
    public enum TelemetryType
    {
        ApplicationInsights = 1,
        // Add future telemetry types here, e.g., NewTelemetryProvider = 2
    }

    /// <summary>
    /// This class is responsible for creating the telemetry provider based on the configuration.
    /// We can add different providers here in the future, instead of Application Insights.
    /// </summary>
    public static class TelemetryProviderFactory
    {
        /// <summary>
        /// Creates the telemetry provider based on the specified telemetry type and configuration.
        /// </summary>
        /// <param name="telemetryType">The telemetry type as an enum.</param>
        /// <param name="configuration">The configuration for the telemetry provider.</param>
        /// <returns>An instance of ITelemetryProvider.</returns>
        public static ITelemetryProvider CreateTelemetryProvider(TelemetryType telemetryType, Dictionary<string, string> configuration)
        {
            return telemetryType switch
            {
                TelemetryType.ApplicationInsights => new ApplicationInsightsTelemetryProvider(configuration),
                _ => throw new NotSupportedException($"Telemetry type '{telemetryType}' is not supported.")
            };
        }
    }
}