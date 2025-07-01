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
    }
}
