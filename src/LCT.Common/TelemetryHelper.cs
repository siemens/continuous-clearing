// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using LCT.Telemetry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace LCT.Common
{
    public class TelemetryHelper
    {
        public static void InitializeAndTrackEvent(LCT.Telemetry.Telemetry telemetry, string toolName, string toolVersion, string eventName,
                                                    CommonAppSettings appSettings)
        {
            telemetry.Initialize(toolName, toolVersion);

            telemetry.TrackCustomEvent(eventName, new Dictionary<string, string>
            {
                { "CA Tool Version", toolVersion },
                { "SW360 Project Name", appSettings.SW360.ProjectName },
                { "SW360 Project ID", appSettings.SW360.ProjectID },
                { "Project Type", appSettings.ProjectType },
                { "Hashed User ID", HashUtility.GetHashString(Environment.UserName) },
                { "Start Time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) }
            });
        }

        public static void TrackKpiDataTelemetry<T>(LCT.Telemetry.Telemetry telemetry, string eventName, T kpiData)
        {
            var properties = typeof(T).GetProperties();
            var telemetryData = properties.ToDictionary(
                prop => prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? prop.Name,
                prop => prop.GetValue(kpiData)?.ToString()
            );

            telemetryData["Hashed User ID"] = HashUtility.GetHashString(Environment.UserName);
            telemetryData["Time stamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            telemetry.TrackCustomEvent(eventName, telemetryData);
        }

        public static void TrackException(LCT.Telemetry.Telemetry telemetry, Exception ex)
        {
            var exceptionData = new Dictionary<string, string>
        {
            { "Error Time", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
            { "Stack Trace", ex.StackTrace }
        };

            telemetry.TrackException(ex, exceptionData);
        }
    }
}