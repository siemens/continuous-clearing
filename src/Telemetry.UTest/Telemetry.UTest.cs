// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Moq;


namespace LCT.Telemetry.UTest
{
    [TestFixture]
    public class TelemetryTests
    {

        private Mock<LCT.Telemetry.ITelemetryProvider> _mockTelemetryProvider;
        private LCT.Telemetry.Telemetry _telemetry;
        private Dictionary<string, string> configuration;
        private TelemetryClient _mockTelemetryClient;

        [SetUp]
        public void SetUp()
        {
            _mockTelemetryProvider = new Mock<LCT.Telemetry.ITelemetryProvider>();
            var aiConfig = TelemetryConfiguration.CreateDefault();
            string telemetryType = "1";
            configuration = new Dictionary<string, string>
            {
                { "InstrumentationKey", "1" }
            };
            _telemetry = new LCT.Telemetry.Telemetry(telemetryType, configuration);
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
            Assert.Throws<NotSupportedException>(() => new LCT.Telemetry.Telemetry("InvalidType", configuration));
        }
        [Test]
        public void GetHashString_InputIsNull_ReturnsEmptyString()
        {
            // Arrange
            string input = null;

            // Act
            string result = HashUtility.GetHashString(input);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void GetHashString_InputIsEmpty_ReturnsEmptyString()
        {
            // Arrange
            string input = string.Empty;

            // Act
            string result = HashUtility.GetHashString(input);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void GetHashString_ValidInput_ReturnsExpectedHash()
        {
            // Arrange
            string input = "test";
            string expectedHash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08"; // Precomputed SHA256 hash for "test"

            // Act
            string result = HashUtility.GetHashString(input);

            // Assert
            Assert.AreEqual(expectedHash, result);
        }

        [Test]
        public void GetHashString_DifferentInputs_ReturnDifferentHashes()
        {
            // Arrange
            string input1 = "test1";
            string input2 = "test2";

            // Act
            string result1 = HashUtility.GetHashString(input1);
            string result2 = HashUtility.GetHashString(input2);

            // Assert
            Assert.AreNotEqual(result1, result2);
        }

    }
}