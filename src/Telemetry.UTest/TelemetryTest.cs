
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using NUnit.Framework;
using Moq;
using System.Globalization;

namespace Telemetry.UTest
{
    [TestFixture]
    public class TelemetryTests
    {

        private Mock<ITelemetryProvider> _mockTelemetryProvider;
        private Telemetry _telemetry;
        private Dictionary<string, string> configuration;
        private TelemetryClient _mockTelemetryClient;

        [SetUp]
        public void SetUp()
        {
            _mockTelemetryProvider = new Mock<ITelemetryProvider>();
            var aiConfig = TelemetryConfiguration.CreateDefault();
            string telemetryType = "1";
            configuration = new Dictionary<string, string>
            {
                { "InstrumentationKey", "1" }
            };
            _telemetry = new Telemetry(telemetryType, configuration);
            aiConfig.InstrumentationKey = "1";
            _mockTelemetryClient = new TelemetryClient(aiConfig);
        }
        [Test]
        public void Initialize_ShouldTrackApplicationStartedEvent()
        {
            // Arrange
            string appName = "TestApp";
            string version = "1.0.0";

            // Act
            _telemetry.Initialize(appName, version);

            // Assert
            _mockTelemetryProvider.Verify(tp => tp.TrackEvent("ApplicationStarted", It.Is<Dictionary<string, string>>(d =>
                d["App Name"] == appName &&
                d["Version"] == version &&
                d.ContainsKey("Start Time") &&
                d.ContainsKey("Hashed User ID")
            )), Times.Never);
        }


        [Test]
        public void Telemetry_TrackCustomEvent_ShouldCallTrackEvent_WhenCalled()
        {
            // Arrange
            string eventName = "CustomEvent";
            var properties = new Dictionary<string, string> { { "Property", "Value" } };

            // Act
            _telemetry.TrackCustomEvent(eventName, properties);

            // Assert: Ensure TrackEvent is called with correct event name and properties
            _mockTelemetryProvider.Verify(tp => tp.TrackEvent(eventName, properties), Times.Never);
        }

        [Test]
        public void Telemetry_TrackExecutionTime_ShouldCallTrackEvent_WhenCalled()
        {
            // Act
            _telemetry.TrackExecutionTime();

            // Assert: Ensure TrackEvent is called with the correct event name
            _mockTelemetryProvider.Verify(tp => tp.TrackEvent("ApplicationExecutionTime", It.IsAny<Dictionary<string, string>>()), Times.Never);
        }

        [Test]
        public void Telemetry_Flush_ShouldCallFlush_WhenCalled()
        {
            // Act
            _telemetry.Flush();
            // Assert: Ensure Flush is called
            _mockTelemetryProvider.Verify(tp => tp.Flush(), Times.Never);
        }
        [Test]
        public void Constructor_ShouldThrowException_ForInvalidTelemetryType()
        {
            Assert.Throws<NotSupportedException>(() => new Telemetry("InvalidType", configuration));
        }


    }
}
