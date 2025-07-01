// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using LCT.Common.Constants;
using LCT.Telemetry;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LCT.Common
{
    public class TelemetryHelper
    {
        private readonly ILog Logger;
        private readonly LCT.Telemetry.Telemetry telemetry_;
        private readonly EnvironmentHelper environmentHelper = new EnvironmentHelper();
        private readonly CommonAppSettings appSettings_;

        public TelemetryHelper(CommonAppSettings appSettings)
        {            
            Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            appSettings_ = appSettings ?? new CommonAppSettings();

            telemetry_ = new LCT.Telemetry.Telemetry(TelemetryConstant.Type, new Dictionary<string, string>
                {
                { "InstrumentationKey", appSettings?.Telemetry?.ApplicationInsightInstrumentKey ?? string.Empty }
            });
        }

        public void StartTelemetry<T>(string catoolVersion, T kpiData, string telemetryFor)
        {
            // Initialize telemetry with CATool version and instrumentation key only if Telemetry is enabled in appsettings
            Logger.Warn(TelemetryConstant.StartLogMessage);
            try
            {
                InitializeAndTrackEvent(TelemetryConstant.ToolName, catoolVersion, telemetryFor
                                                    , appSettings_);
                TrackKpiDataTelemetry(telemetryFor, kpiData);
            }            
            catch (Exception ex ) when (ex is ArgumentNullException or IOException)
            {
                Logger.Error($"An error occurred: {ex.Message}");
                TrackException(ex);
                environmentHelper.CallEnvironmentExit(-1);
            }
            finally
            {
                telemetry_.Flush(); // Ensure telemetry is sent before application exits
            }
        }

        private void InitializeAndTrackEvent(string toolName, string toolVersion, string eventName,
                                                    CommonAppSettings appSettings)
        {
            telemetry_.Initialize(toolName, toolVersion);

            telemetry_.TrackCustomEvent(eventName, new Dictionary<string, string>
            {
                { "CA Tool Version", toolVersion },
                { "SW360 Project Name", appSettings?.SW360?.ProjectName },
                { "SW360 Project ID", appSettings?.SW360?.ProjectID },
                { "Project Type", appSettings?.ProjectType },
                { "Hashed User ID", HashUtility.GetHashString(Environment.UserName) },
                { "Start Time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) }
            });
        }
        private void TrackKpiDataTelemetry<T>(string eventName, T kpiData)
        {
            var properties = typeof(T).GetProperties();
            var telemetryData = properties.ToDictionary(
                prop => prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? prop.Name,
                prop => prop.GetValue(kpiData)?.ToString()
            );

            telemetryData["Hashed User ID"] = HashUtility.GetHashString(Environment.UserName);
            telemetryData["Time stamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            telemetry_.TrackCustomEvent(eventName, telemetryData);
        }

        private void TrackException(Exception ex)
        {
            var exceptionData = new Dictionary<string, string>
        {
            { "Error Time", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
            { "Stack Trace", ex.StackTrace }
        };

            telemetry_.TrackException(ex, exceptionData);
        }
    }
}