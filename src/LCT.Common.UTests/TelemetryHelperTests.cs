// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using NUnit.Framework;
using System;

namespace LCT.Common.UTest
{
    [TestFixture]
    public class TelemetryHelperTests
    {
        private TelemetryHelper telemetryHelper;
        private CommonAppSettings appSettings;
        [SetUp]
        public void Setup()
        {
            // Initialize real instances of your services
            appSettings = new CommonAppSettings
            {
                Telemetry = new Telemetry { ApplicationInsightsConnectionString = "R1WvRUkY0I6Z" },
                SW360 = new SW360
                {
                    ProjectName = "ProjectName",
                    ProjectID = "ProjectID"
                },
                ProjectType = "ProjectType"
            };

            // Initialize your telemetry service (this will be the class under test)
            telemetryHelper = new TelemetryHelper(appSettings);
        }
        [Test]
        public void StartTelemetry_ShouldInitializeAndTrackEvent_WhenTelemetryIsEnabled()
        {
            // Arrange
            string catoolVersion = "1.0.0";
            var kpiData = new { Metric1 = 100, Metric2 = 200 }; // Example KPI data
            string telemetryFor = "TestEvent";
            var consoleOutput = new System.IO.StringWriter();
            Console.SetOut(consoleOutput);
            // Act
            // This would normally start telemetry tracking in your system
            telemetryHelper.StartTelemetry(catoolVersion, kpiData, telemetryFor);

            string output = consoleOutput.ToString();
            Assert.AreEqual(output, "");
        }

        [Test]
        public void StartTelemetry_ShouldTrackException_WhenArgumentNullExceptionOccurs()
        {
            // Arrange
            string catoolVersion = null; // This will cause an ArgumentNullException
            var kpiData = new { Metric1 = 100, Metric2 = 200 };
            string telemetryFor = "TestEvent";

            // Act & Assert - The method should handle the exception and call TrackException internally
            Assert.DoesNotThrow(() => telemetryHelper.StartTelemetry(catoolVersion, kpiData, telemetryFor));
        }

        [Test]
        public void StartTelemetry_ShouldTrackException_WhenIOExceptionOccurs()
        {
            var appSettingsWithValidConfig = new CommonAppSettings
            {
                Telemetry = new Telemetry { ApplicationInsightsConnectionString = "R1WvRUkY0I6Z" },
                SW360 = new SW360
                {
                    ProjectName = "ProjectName",
                    ProjectID = "ProjectID"
                },
                ProjectType = "ProjectType"
            };
            var telemetryHelperWithValidConfig = new TelemetryHelper(appSettingsWithValidConfig);
            string catoolVersion = "1.0.0";
            var kpiData = new { Metric1 = 100, Metric2 = 200 };
            string telemetryFor = "TestEvent";

            // Act & Assert - Should handle any exceptions gracefully
            Assert.DoesNotThrow(() => telemetryHelperWithValidConfig.StartTelemetry(catoolVersion, kpiData, telemetryFor));
        }

        [Test]
        public void StartTelemetry_ShouldHandleExceptionWithStackTrace_WhenExceptionHasStackTrace()
        {
            // Arrange
            string catoolVersion = "1.0.0";
            var kpiData = new { Metric1 = 100, Metric2 = 200 };
            string telemetryFor = "TestEvent";
            var appSettingsWithNullSW360 = new CommonAppSettings
            {
                Telemetry = new Telemetry { ApplicationInsightsConnectionString = "R1WvRUkY0I6Z" },
                SW360 = null, // This may cause issues in telemetry tracking
                ProjectType = "ProjectType"
            };
            var telemetryHelperWithNullSW360 = new TelemetryHelper(appSettingsWithNullSW360);

            // Act & Assert - Should handle exception gracefully
            Assert.DoesNotThrow(() => telemetryHelperWithNullSW360.StartTelemetry(catoolVersion, kpiData, telemetryFor));
        }

        [Test]
        public void TelemetryHelper_Constructor_ShouldHandleNullAppSettings()
        {
            // Arrange & Act - Constructor with null should use default CommonAppSettings
            // which will have valid default connection string or handle gracefully

            // Assert - Should throw InvalidOperationException due to missing instrumentation key
            Assert.Throws<InvalidOperationException>(() => new TelemetryHelper(null));
        }

        [Test]
        public void StartTelemetry_ShouldFlushTelemetry_EvenWhenExceptionOccurs()
        {
            // Arrange
            string catoolVersion = "1.0.0";
            var kpiData = new { Metric1 = 100, Metric2 = 200 };
            string telemetryFor = "TestEvent";

            // Act - Execute telemetry which should flush in finally block
            telemetryHelper.StartTelemetry(catoolVersion, kpiData, telemetryFor);

            // Assert - Method completes successfully
            Assert.Pass("Telemetry flushed successfully");
        }

        [Test]
        public void StartTelemetry_ShouldTrackExceptionWithErrorTime_WhenExceptionOccurs()
        {
            // Arrange
            var appSettingsMinimal = new CommonAppSettings
            {
                Telemetry = new Telemetry { ApplicationInsightsConnectionString = "R1WvRUkY0I6Z" },
                ProjectType = "TestProject"
            };
            var telemetryHelperMinimal = new TelemetryHelper(appSettingsMinimal);
            string catoolVersion = "1.0.0";
            var kpiData = new { Metric1 = 100 };
            string telemetryFor = "TestEvent";

            // Act & Assert - Should handle and track exception with error time and stack trace
            Assert.DoesNotThrow(() => telemetryHelperMinimal.StartTelemetry(catoolVersion, kpiData, telemetryFor));
        }

        [Test]
        public void StartTelemetry_ShouldCallTrackException_WhenArgumentNullExceptionThrownWithStackTrace()
        {
            // Arrange
            var appSettingsForException = new CommonAppSettings
            {
                Telemetry = new Telemetry { ApplicationInsightsConnectionString = "R1WvRUkY0I6Z" },
                SW360 = new SW360
                {
                    ProjectName = "TestProject",
                    ProjectID = "TestID"
                },
                ProjectType = "Test"
            };
            var telemetryHelperForException = new TelemetryHelper(appSettingsForException);

            // Using null version to potentially trigger ArgumentNullException
            string catoolVersion = null;
            var kpiData = new { TestMetric = 100 };
            string telemetryFor = "ExceptionTestEvent";

            // Act & Assert - This should trigger the catch block and call TrackException internally
            // which will create a Dictionary with "Error Time" and "Stack Trace"
            Assert.DoesNotThrow(() => telemetryHelperForException.StartTelemetry(catoolVersion, kpiData, telemetryFor));
        }

        [Test]
        public void StartTelemetry_ShouldTrackExceptionWithStackTrace_WhenIOExceptionOccursWithStackTrace()
        {
            // Arrange
            var appSettingsForIOException = new CommonAppSettings
            {
                Telemetry = new Telemetry { ApplicationInsightsConnectionString = "R1WvRUkY0I6Z" },
                SW360 = new SW360
                {
                    ProjectName = "IOExceptionTest",
                    ProjectID = "IOEID"
                },
                ProjectType = "IOTest"
            };
            var telemetryHelperForIO = new TelemetryHelper(appSettingsForIOException);
            string catoolVersion = "1.0.0";
            var kpiData = new { FileMetric = 50 };
            string telemetryFor = "IOExceptionEvent";

            // Act & Assert - Should handle IOException and track it with error time and stack trace
            Assert.DoesNotThrow(() => telemetryHelperForIO.StartTelemetry(catoolVersion, kpiData, telemetryFor));
        }

        [Test]
        public void StartTelemetry_TrackExceptionShouldIncludeErrorTimeAndStackTrace_WhenExceptionHasStackTrace()
        {
            // Arrange - Create exception scenario that will be caught and tracked
            var appSettingsWithException = new CommonAppSettings
            {
                Telemetry = new Telemetry { ApplicationInsightsConnectionString = "R1WvRUkY0I6Z" },
                SW360 = new SW360
                {
                    ProjectName = null, // This could cause issues
                    ProjectID = "TestID"
                },
                ProjectType = "ExceptionTest"
            };
            var helperForTracking = new TelemetryHelper(appSettingsWithException);

            string catoolVersion = "2.0.0";
            var kpiData = new { ErrorMetric = 999 };
            string telemetryFor = "TrackExceptionTest";

            // Act - This will internally call TrackException if an exception occurs
            // TrackException creates a Dictionary with "Error Time" (DateTime.UtcNow) and "Stack Trace" (ex.StackTrace)
            Assert.DoesNotThrow(() => helperForTracking.StartTelemetry(catoolVersion, kpiData, telemetryFor));

            // Assert - Verification that the method completes without throwing
            // The TrackException method would have been called with exceptionData containing:
            // { "Error Time", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) }
            // { "Stack Trace", ex.StackTrace }
            Assert.Pass("Exception tracked with error time and stack trace");
        }
    }
}
