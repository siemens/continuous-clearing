namespace LCT.Telemetry
{
    public interface ITelemetryProvider
    {
        void TrackEvent(string eventName, Dictionary<string, string> properties = null);
        void TrackException(Exception ex, Dictionary<string, string> properties = null);
        void Flush();
    }
}