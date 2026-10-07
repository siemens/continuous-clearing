// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using System.Collections.Generic;

namespace SIT.Telemetry.UTest
{
    [TestFixture]
    public class TelemetryTests
    {
        private SIT.Telemetry.Telemetry _telemetry;
        private Dictionary<string, string> configuration;

        [SetUp]
        public void SetUp()
        {
            configuration = new Dictionary<string, string>
            {
                { "ConnectionString", "InstrumentationKey=00000000-0000-0000-0000-000000000000" }
            };
            _telemetry = new SIT.Telemetry.Telemetry("ApplicationInsights", configuration);
        }

        [Test]
        public void Initialize_ShouldNotThrow_WhenCalled()
        {
            // Arrange
            string appName = "TestApp";
            string version = "1.0.0";

            // Act & Assert
            Assert.DoesNotThrow(() => _telemetry.Initialize(appName, version));
        }

        [Test]
        public void Telemetry_TrackCustomEvent_ShouldNotThrow_WhenCalled()
        {
            // Arrange
            string eventName = "CustomEvent";
            var properties = new Dictionary<string, string> { { "Property", "Value" } };

            // Act & Assert
            Assert.DoesNotThrow(() => _telemetry.TrackCustomEvent(eventName, properties));
        }

        [Test]
        public void Telemetry_TrackExecutionTime_ShouldNotThrow_WhenCalledWithoutInitialize()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _telemetry.TrackExecutionTime());
        }

        [Test]
        public void Telemetry_TrackExecutionTime_ShouldNotThrow_WhenCalledAfterInitialize()
        {
            // Arrange
            _telemetry.Initialize("TestApp", "1.0.0");

            // Act & Assert
            Assert.DoesNotThrow(() => _telemetry.TrackExecutionTime());
        }

        [Test]
        public void Telemetry_Flush_ShouldNotThrow_WhenCalled()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _telemetry.Flush());
        }

        [Test]
        public void Constructor_ShouldThrowException_ForInvalidTelemetryType()
        {
            Assert.Throws<NotSupportedException>(() => new SIT.Telemetry.Telemetry("InvalidType", configuration));
        }

        [Test]
        public void Constructor_ShouldThrowException_WhenConnectionStringMissing()
        {
            var emptyConfiguration = new Dictionary<string, string>();
            Assert.Throws<InvalidOperationException>(() => new SIT.Telemetry.Telemetry("ApplicationInsights", emptyConfiguration));
        }

        [Test]
        public void GetHashString_InputIsNull_ReturnsEmptyString()
        {
            // Arrange
            string? input = null;

            // Act
            string result = HashUtility.GetHashString(input ?? string.Empty);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetHashString_InputIsEmpty_ReturnsEmptyString()
        {
            // Arrange
            string input = string.Empty;

            // Act
            string result = HashUtility.GetHashString(input);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
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
            Assert.That(result, Is.EqualTo(expectedHash));
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
            Assert.That(result1, Is.Not.EqualTo(result2));
        }

    }
}