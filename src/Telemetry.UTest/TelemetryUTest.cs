using NUnit.Framework;
using Moq;
using Telemetry;
using System;
using System.Collections.Generic;
using Telemetry.UTest;

namespace Telemetry.Tests
{
    [TestFixture]
    public class TelemetryTests
    {
        private Mock<ITelemetryProvider> _mockTelemetryProvider;
        private Dictionary<string, string> _configuration;
        private Telemetry _telemetry;

        [SetUp]
        public void Setup()
        {
            _mockTelemetryProvider = new Mock<ITelemetryProvider>();
            _configuration = new Dictionary<string, string> { { "key", "value" } };

            // Mock TelemetryProviderFactory.CreateTelemetryProvider
            TelemetryProviderFactory.CreateTelemetryProvider = (type, config) =>
            {
                if (config == null)
                {
                    throw new ArgumentNullException(nameof(config));
                }
                return _mockTelemetryProvider.Object;
            };
        }

        [Test]
        public void Constructor_ShouldThrowException_ForInvalidTelemetryType()
        {
            Assert.Throws<NotSupportedException>(() => new Telemetry("InvalidType", _configuration));
        }

        [Test]
        public void Constructor_ShouldInitialize_ForValidTelemetryType()
        {
            Assert.DoesNotThrow(() => new Telemetry("Custom", _configuration));
        }

        [Test]
        public void Initialize_ShouldTrackApplicationStartedEvent()
        {
            var appName = "TestApp";
            var version = "1.0";

            _telemetry = new Telemetry("Custom", _configuration);
            _telemetry.Initialize(appName, version);

            _mockTelemetryProvider.Verify(tp => tp.TrackEvent("ApplicationStarted", It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        [Test]
        public void TrackCustomEvent_ShouldCallTrackEvent()
        {
            var eventName = "CustomEvent";
            var properties = new Dictionary<string, string> { { "Key", "Value" } };

            _telemetry = new Telemetry("Custom", _configuration);
            _telemetry.TrackCustomEvent(eventName, properties);

            _mockTelemetryProvider.Verify(tp => tp.TrackEvent(eventName, properties), Times.Once);
        }

        [Test]
        public void TrackException_ShouldCallTrackException()
        {
            var exception = new Exception("Test Exception");
            var properties = new Dictionary<string, string> { { "ErrorKey", "ErrorValue" } };

            _telemetry = new Telemetry("Custom", _configuration);
            _telemetry.TrackException(exception, properties);

            _mockTelemetryProvider.Verify(tp => tp.TrackException(exception, properties), Times.Once);
        }

        [Test]
        public void TrackExecutionTime_ShouldTrackExecutionTimeEvent()
        {
            _telemetry = new Telemetry("Custom", _configuration);

            _telemetry.Initialize("TestApp", "1.0"); // Starts the stopwatch
            _telemetry.TrackExecutionTime();

            _mockTelemetryProvider.Verify(tp => tp.TrackEvent("ApplicationExecutionTime", It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        [Test]
        public void Flush_ShouldCallFlushOnTelemetryProvider()
        {
            _telemetry = new Telemetry("Custom", _configuration);

            _telemetry.Flush();

            _mockTelemetryProvider.Verify(tp => tp.Flush(), Times.Once);
        }

        [Test]
        public void Initialize_ShouldTrackUserDetails()
        {
            _telemetry = new Telemetry("Custom", _configuration);
            _telemetry.Initialize("TestApp", "1.0");

            _mockTelemetryProvider.Verify(tp => tp.TrackEvent("UserDetails", It.IsAny<Dictionary<string, string>>()), Times.Once);
        }
    }
}
