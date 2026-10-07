// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using System.Globalization;
using Telemetry.Uploader;

/// <summary>
/// Telemtetry class to track custom events, exceptions and execution time
/// </summary>

namespace SIT.Telemetry
{
    /// <summary>
    /// Telemetry class to track custom events, exceptions and execution time using the Telemetry.Uploader SDK.
    /// </summary>
    public class Telemetry
    {
        #region Fields
        private readonly ITelemetryService _telemetryService;
        private TelemetryScope? _executionScope;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Telemetry"/> class.
        /// </summary>
        /// <param name="telemetryType">The type of telemetry provider to use. Only "ApplicationInsights" is supported.</param>
        /// <param name="configuration">The configuration dictionary containing connection string and standard telemetry metadata.</param>
        /// <exception cref="NotSupportedException">Thrown when the telemetry type is not supported.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the configuration is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the Application Insights connection string is missing or invalid.</exception>
        public Telemetry(string telemetryType, Dictionary<string, string> configuration)
        {
            if (!string.Equals(telemetryType, "ApplicationInsights", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException($"Telemetry type '{telemetryType}' is not supported.");
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null.");
            }

            var connectionString = configuration.GetValueOrDefault("ConnectionString")
                                 ?? Environment.GetEnvironmentVariable("TELEMETRY_CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Application Insights Instrumentation Key is missing or invalid.");
            }

            var options = new TelemetryOptions
            {
                ConnectionString = connectionString,
                Product = configuration.GetValueOrDefault("Product") ?? string.Empty,
                Application = configuration.GetValueOrDefault("Application") ?? string.Empty,
                Component = configuration.GetValueOrDefault("Component") ?? string.Empty,
                Environment = configuration.GetValueOrDefault("Environment") ?? string.Empty,
                Version = configuration.GetValueOrDefault("Version") ?? string.Empty,
                Organization = configuration.GetValueOrDefault("Organization") ?? string.Empty
            };

            _telemetryService = new ApplicationInsightsTelemetryService(options);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Initializes the telemetry tracking with application information and starts the execution time operation scope.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <param name="version">The version of the application.</param>
        public void Initialize(string appName, string version)
        {
            _executionScope = _telemetryService.StartOperation("ApplicationExecutionTime", new Dictionary<string, string>
            {
                { "App Name", appName },
                { "Version", version },
                { "Start Time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
                { "Hashed User ID", HashUtility.GetHashString(Environment.UserName) }
            });

            _telemetryService.TrackEvent("ApplicationStarted", new Dictionary<string, string>
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
            _executionScope?.Fail(ex);
        }

        /// <summary>
        /// Completes the execution time operation scope, tracking total duration and end time.
        /// </summary>
        public void TrackExecutionTime()
        {
            if (_executionScope == null)
            {
                return;
            }

            _executionScope.SetMessage($"End Time: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
            _executionScope.Dispose();
            _executionScope = null;
        }

        /// <summary>
        /// Flushes the telemetry service to ensure all telemetry data is sent.
        /// </summary>
        public void Flush()
        {
            _telemetryService.Flush();
            Thread.Sleep(1000); // Allow some time for telemetry to be sent
        }

        /// <summary>
        /// Tracks user details including hashed user ID, machine name, and login time.
        /// </summary>
        private void TrackUserDetails()
        {
            string userName = HashUtility.GetHashString(Environment.UserName);
            string machineName = Environment.MachineName;

            _telemetryService.TrackEvent("UserDetails", new Dictionary<string, string>
            {
                { "User Id", userName },
                { "Machine Name", machineName },
                { "Login Time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) }
            });
        }
        #endregion
    }
}