// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using System.Diagnostics;
using System.Globalization;

/// <summary>
/// Telemtetry class to track custom events, exceptions and execution time
/// </summary>

namespace LCT.Telemetry
{
    /// <summary>
    /// Telemetry class to track custom events, exceptions and execution time.
    /// </summary>
    public class Telemetry
    {
        #region Fields
        private readonly ITelemetryProvider _telemetryProvider;
        private readonly Stopwatch _stopwatch;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Telemetry"/> class.
        /// </summary>
        /// <param name="telemetryType">The type of telemetry provider to use.</param>
        /// <param name="configuration">The configuration dictionary for the telemetry provider.</param>
        /// <exception cref="NotSupportedException">Thrown when the telemetry type is not supported.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the configuration is null.</exception>
        public Telemetry(string telemetryType, Dictionary<string, string> configuration)
        {
            _stopwatch = new Stopwatch();

            if (!Enum.TryParse<TelemetryType>(telemetryType, true, out var parsedTelemetryType))
            {
                throw new NotSupportedException($"Telemetry type '{telemetryType}' is not supported.");
            }

            _telemetryProvider = TelemetryProviderFactory.CreateTelemetryProvider(
                parsedTelemetryType,
                configuration ?? throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null.")
            );
        }
        #endregion

        #region Methods
        /// <summary>
        /// Initializes the telemetry tracking with application information and starts the stopwatch.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <param name="version">The version of the application.</param>
        public void Initialize(string appName, string version)
        {
            _stopwatch.Start();

            _telemetryProvider.TrackEvent("ApplicationStarted", new Dictionary<string, string>
            {
                { "App Name", appName },
                { "Version", version },
                { "Start Time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
                { "Hashed User ID", HashUtility.GetHashString(Environment.UserName) }
            });

            TrackUserDetails();
        }

        /// <summary>
        /// Tracks a custom event with optional properties.
        /// </summary>
        /// <param name="eventName">The name of the event to track.</param>
        /// <param name="properties">Optional dictionary of properties to include with the event.</param>
        public void TrackCustomEvent(string eventName, Dictionary<string, string>? properties = null)
        {
            _telemetryProvider.TrackEvent(eventName, properties);
        }

        /// <summary>
        /// Tracks an exception with optional properties.
        /// </summary>
        /// <param name="ex">The exception to track.</param>
        /// <param name="properties">Optional dictionary of properties to include with the exception.</param>
        public void TrackException(Exception ex, Dictionary<string, string>? properties = null)
        {
            _telemetryProvider.TrackException(ex, properties);
        }

        /// <summary>
        /// Tracks the execution time of the application and stops the stopwatch.
        /// </summary>
        public void TrackExecutionTime()
        {
            _stopwatch.Stop();

            _telemetryProvider.TrackEvent("ApplicationExecutionTime", new Dictionary<string, string>
            {
                 { "Hashed User ID", HashUtility.GetHashString(Environment.UserName) },
                { "Total Execution Time", $"{_stopwatch.Elapsed.TotalSeconds} seconds" },
                { "End Time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) }
            });
        }

        /// <summary>
        /// Flushes the telemetry provider to ensure all telemetry data is sent.
        /// </summary>
        public void Flush()
        {
            _telemetryProvider.Flush();
        }

        /// <summary>
        /// Tracks user details including hashed user ID, machine name, and login time.
        /// </summary>
        private void TrackUserDetails()
        {
            string userName = HashUtility.GetHashString(Environment.UserName);
            string machineName = Environment.MachineName;

            _telemetryProvider.TrackEvent("UserDetails", new Dictionary<string, string>
            {
                { "User Id", userName },
                { "Machine Name", machineName },
                { "Login Time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) }
            });
        }
        #endregion
    }
}