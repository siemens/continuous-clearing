using System.Diagnostics;
using System.Globalization;

/// <summary>
/// Telemtetry class to track custom events, exceptions and execution time
/// </summary>

namespace LCT.Telemetry
{
    public class Telemetry
    {
        private readonly ITelemetryProvider _telemetryProvider;
        private readonly Stopwatch _stopwatch;

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

        public void TrackCustomEvent(string eventName, Dictionary<string, string> properties = null)
        {
            _telemetryProvider.TrackEvent(eventName, properties);
        }

        public void TrackException(Exception ex, Dictionary<string, string> properties = null)
        {
            _telemetryProvider.TrackException(ex, properties);
        }

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

        public void Flush()
        {
            _telemetryProvider.Flush();
        }

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
    }
}