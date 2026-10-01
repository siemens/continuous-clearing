// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using log4net;
using SIT.Common.Constants;
using SIT.Common.Logging;
using SIT.Telemetry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SIT.Common
{
    public class TelemetryHelper
    {
        #region Fields

        private readonly ILog Logger;
        private readonly SIT.Telemetry.Telemetry telemetry_;
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

            telemetry_ = new SIT.Telemetry.Telemetry(TelemetryConstant.Type, new Dictionary<string, string>
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
        /// <param name="appDataEventName">The telemetry event name used for the application/context event.</param>
        /// <param name="kpiEventName">The telemetry event name used for the KPI data event.</param>
        /// <param name="timeTaken">Total time taken by the tool to complete execution.</param>
        /// <param name="stage">The pipeline stage identifier (e.g. scan, create, upload) used to tag all emitted metrics/events.</param>
        public void StartTelemetry<T>(string catoolVersion, T kpiData, string appDataEventName, string kpiEventName, TimeSpan? timeTaken = null, string stage = null)
        {
            // Initialize telemetry with CATool version and instrumentation key only if Telemetry is enabled in appsettings
            LoggerHelper.WriteTelemetryMessage(TelemetryConstant.StartLogMessage);
            var commonAttributes = BuildCommonAttributes(catoolVersion, stage, TelemetryConstant.StatusSuccess);
            try
            {
                InitializeAndTrackEvent(TelemetryConstant.ToolName, catoolVersion, appDataEventName
                                                    , appSettings_, timeTaken);
                TrackKpiDataTelemetry(kpiEventName, kpiData, commonAttributes);
                TrackStageMetrics(stage, timeTaken, commonAttributes);
            }
            catch (Exception ex) when (ex is ArgumentNullException or IOException)
            {
                LogHandlingHelper.ExceptionErrorHandling("Exception", "StartTelemetry()", ex, "");
                Logger.ErrorFormat("An error occurred: {0}", ex.Message);
                var failureAttributes = new Dictionary<string, string>(commonAttributes)
                {
                    [TelemetryConstant.AttrStatus] = TelemetryConstant.StatusFailure
                };
                TrackException(ex, failureAttributes);
                telemetry_.TrackMetric(TelemetryConstant.MetricStageExecutionCount, 1, failureAttributes);
                environmentHelper.CallEnvironmentExit(-1);
            }
            finally
            {
                telemetry_.Flush(); // Ensure telemetry is sent before application exits
            }
        }

        /// <summary>
        /// Builds the common, low-cardinality attribute set shared by all metrics/events for a single tool execution.
        /// </summary>
        private Dictionary<string, string> BuildCommonAttributes(string catoolVersion, string stage, string status)
        {
            return new Dictionary<string, string>
            {
                { TelemetryConstant.AttrStage, stage ?? string.Empty },
                { TelemetryConstant.AttrProjectName, appSettings_?.SW360?.ProjectName ?? string.Empty },
                { TelemetryConstant.AttrProjectId, appSettings_?.SW360?.ProjectID ?? string.Empty },
                { TelemetryConstant.AttrRunId, SIT.Telemetry.Telemetry.ResolveRunId(TelemetryConstant.RunIdEnvironmentVariable) },
                { TelemetryConstant.AttrToolVersion, catoolVersion ?? string.Empty },
                { TelemetryConstant.AttrStatus, status },
                { TelemetryConstant.AttrPackageType, appSettings_?.ProjectType ?? string.Empty },
                { TelemetryConstant.AttrUserHash, HashUtility.GetHashString(Environment.UserName) }
            };
        }

        /// <summary>
        /// Tracks stage-level duration and execution-count metrics tagged with the common attributes.
        /// </summary>
        private void TrackStageMetrics(string stage, TimeSpan? timeTaken, Dictionary<string, string> commonAttributes)
        {
            if (string.IsNullOrEmpty(stage))
            {
                return;
            }

            if (timeTaken.HasValue)
            {
                telemetry_.TrackMetric(TelemetryConstant.MetricStageDuration, timeTaken.Value.TotalSeconds, commonAttributes);
            }

            telemetry_.TrackMetric(TelemetryConstant.MetricStageExecutionCount, 1, commonAttributes);
        }

        /// <summary>
        /// Initializes telemetry and tracks a custom event with application details.
        /// </summary>
        /// <param name="toolName">The name of the tool.</param>
        /// <param name="toolVersion">The version of the tool.</param>
        /// <param name="eventName">The name of the event to track.</param>
        /// <param name="appSettings">The common application settings.</param>
        private void InitializeAndTrackEvent(string toolName, string toolVersion, string eventName,
                                                    CommonAppSettings appSettings, TimeSpan? timeTaken = null)
        {
            telemetry_.Initialize(toolName, toolVersion);

            telemetry_.TrackCustomEvent(eventName, new Dictionary<string, string>
            {
                { "CA Tool Version", toolVersion },
                { "SW360 Project Name", appSettings?.SW360?.ProjectName },
                { "SW360 Project ID", appSettings?.SW360?.ProjectID },
                { "SBOM File Name", Path.GetFileName(FileOperations.CatoolBomFilePath ?? string.Empty) },
                { "Project Type", appSettings?.ProjectType },
                { "Hashed User ID", HashUtility.GetHashString(Environment.UserName) },
                { "Start Time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
                { "Time Taken For Completion", timeTaken.HasValue ? $"{timeTaken.Value.TotalSeconds:0.##} seconds" : string.Empty }
            });
        }

        /// <summary>
        /// Tracks KPI data: numeric properties are emitted as individual metrics (tagged with the common attributes),
        /// while a single lightweight completion event is still tracked for backward compatibility/visibility.
        /// </summary>
        /// <typeparam name="T">The type of KPI data.</typeparam>
        /// <param name="eventName">The name of the event to track.</param>
        /// <param name="kpiData">The KPI data to track.</param>
        /// <param name="commonAttributes">The common attributes to tag every emitted metric with.</param>
        private void TrackKpiDataTelemetry<T>(string eventName, T kpiData, Dictionary<string, string> commonAttributes)
        {
            var properties = typeof(T).GetProperties();
            var telemetryData = properties.ToDictionary(
                prop => prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? prop.Name,
                prop => prop.GetValue(kpiData)?.ToString()
            );

            telemetryData["Hashed User ID"] = HashUtility.GetHashString(Environment.UserName);
            telemetryData["Time stamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            telemetry_.TrackCustomEvent(eventName, telemetryData);

            foreach (var prop in properties)
            {
                if (!IsNumericType(prop.PropertyType))
                {
                    continue;
                }

                var value = prop.GetValue(kpiData);
                if (value == null)
                {
                    continue;
                }

                string metricName = prop.GetCustomAttribute<MetricNameAttribute>()?.Name
                                     ?? $"sit.{(commonAttributes.TryGetValue(TelemetryConstant.AttrStage, out var stage) ? stage : "unknown")}.{prop.Name}".ToLowerInvariant();

                telemetry_.TrackMetric(metricName, Convert.ToDouble(value, CultureInfo.InvariantCulture), commonAttributes);
            }
        }

        /// <summary>
        /// Determines whether the specified type is a numeric type suitable for metric emission.
        /// </summary>
        private static bool IsNumericType(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            return underlyingType == typeof(int) || underlyingType == typeof(long)
                || underlyingType == typeof(double) || underlyingType == typeof(float)
                || underlyingType == typeof(decimal) || underlyingType == typeof(short);
        }

        /// <summary>
        /// Tracks an exception with telemetry data.
        /// </summary>
        /// <param name="ex">The exception to track.</param>
        /// <param name="additionalAttributes">Optional additional attributes to include with the exception.</param>
        private void TrackException(Exception ex, Dictionary<string, string> additionalAttributes = null)
        {
            var exceptionData = new Dictionary<string, string>
        {
            { "Error Time", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
            { "Stack Trace", ex.StackTrace }
        };

            if (additionalAttributes != null)
            {
                foreach (var attribute in additionalAttributes)
                {
                    exceptionData[attribute.Key] = attribute.Value;
                }
            }

            telemetry_.TrackException(ex, exceptionData);
        }

        #endregion
    }
}