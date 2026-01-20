// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using LCT.Common.Constants;
using LCT.Common.Logging;
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
        #region Fields

        private readonly ILog Logger;
        private readonly LCT.Telemetry.Telemetry telemetry_;
        private readonly EnvironmentHelper environmentHelper = new EnvironmentHelper();
        private readonly CommonAppSettings appSettings_;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the TelemetryHelper class.
        /// </summary>
        /// <param name="appSettings">The common application settings.</param>
        public TelemetryHelper(CommonAppSettings appSettings)
        {
            Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            appSettings_ = appSettings ?? new CommonAppSettings();

            telemetry_ = new LCT.Telemetry.Telemetry(TelemetryConstant.Type, new Dictionary<string, string>
                {
                { "ConnectionString", appSettings?.Telemetry?.ApplicationInsightsConnectionString ?? string.Empty }
            });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts telemetry tracking with the specified version and KPI data.
        /// </summary>
        /// <typeparam name="T">The type of KPI data.</typeparam>
        /// <param name="catoolVersion">The CA tool version.</param>
        /// <param name="kpiData">The KPI data to track.</param>
        /// <param name="telemetryFor">The telemetry event name.</param>
        public void StartTelemetry<T>(string catoolVersion, T kpiData, string telemetryFor)
        {
            // Initialize telemetry with CATool version and instrumentation key only if Telemetry is enabled in appsettings
            LoggerHelper.WriteTelemetryMessage(TelemetryConstant.StartLogMessage);
            try
            {
                InitializeAndTrackEvent(TelemetryConstant.ToolName, catoolVersion, telemetryFor
                                                    , appSettings_);
                TrackKpiDataTelemetry(telemetryFor, kpiData);
            }
            catch (Exception ex) when (ex is ArgumentNullException or IOException)
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

        /// <summary>
        /// Initializes telemetry and tracks a custom event with application details.
        /// </summary>
        /// <param name="toolName">The name of the tool.</param>
        /// <param name="toolVersion">The version of the tool.</param>
        /// <param name="eventName">The name of the event to track.</param>
        /// <param name="appSettings">The common application settings.</param>
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

        /// <summary>
        /// Tracks KPI data as a custom telemetry event.
        /// </summary>
        /// <typeparam name="T">The type of KPI data.</typeparam>
        /// <param name="eventName">The name of the event to track.</param>
        /// <param name="kpiData">The KPI data to track.</param>
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

        /// <summary>
        /// Tracks an exception with telemetry data.
        /// </summary>
        /// <param name="ex">The exception to track.</param>
        private void TrackException(Exception ex)
        {
            var exceptionData = new Dictionary<string, string>
        {
            { "Error Time", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
            { "Stack Trace", ex.StackTrace }
        };

            telemetry_.TrackException(ex, exceptionData);
        }

        #endregion
    }
}