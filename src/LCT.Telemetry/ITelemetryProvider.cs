// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
namespace LCT.Telemetry
{
    /// <summary>
    /// Defines a contract for telemetry providers.
    /// </summary>
    public interface ITelemetryProvider
    {
        /// <summary>
        /// Tracks a custom event with optional properties.
        /// </summary>
        /// <param name="eventName">The name of the event to track.</param>
        /// <param name="properties">Optional dictionary of properties to include with the event.</param>
        void TrackEvent(string eventName, Dictionary<string, string>? properties = null);
        
        /// <summary>
        /// Tracks an exception with optional properties.
        /// </summary>
        /// <param name="ex">The exception to track.</param>
        /// <param name="properties">Optional dictionary of properties to include with the exception.</param>
        void TrackException(Exception ex, Dictionary<string, string>? properties = null);
        
        /// <summary>
        /// Flushes the telemetry provider to ensure all telemetry data is sent.
        /// </summary>
        void Flush();
    }
}