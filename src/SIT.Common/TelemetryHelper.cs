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
        private SIT.Telemetry.Telemetry telemetry_;
        private readonly EnvironmentHelper environmentHelper = new EnvironmentHelper();
        private readonly CommonAppSettings appSettings_;
        // KPI: "Pipeline Completion Rate" and "Retry/Re-run Rate". A single Run/Correlation ID is attached to every
        // telemetry event so Grafana/KQL queries can join Scan -> Create -> Upload events for the same execution,
        // and detect repeated runs of the same stage within a short window (re-run/retry detection).
        // If an orchestrator (CI pipeline/script) exports SIT_PIPELINE_RUN_ID before invoking the three tools in
        // sequence, all of them will share the same RunId; otherwise a new GUID is generated for this standalone run.
        private readonly string runId_;

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
            // KPI: "Pipeline Completion Rate" / "Retry Rate" - resolve a shared RunId once per tool invocation.
            // Falls back to a freshly generated GUID when no pipeline orchestrator has supplied a shared one.
            runId_ = Environment.GetEnvironmentVariable(TelemetryConstant.PipelineRunIdEnvironmentVariable)
                     ?? Guid.NewGuid().ToString();
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
        /// <param name="component">The standardized "Component" attribute identifying the SIT tool (e.g. Scan, Create, Upload).</param>
        /// <param name="timeTaken">Total time taken by the tool to complete execution.</param>
        public void StartTelemetry<T>(string catoolVersion, T kpiData, string appDataEventName, string kpiEventName, string component, TimeSpan? timeTaken = null)
        {
            // Initialize telemetry with CATool version and instrumentation key only if Telemetry is enabled in appsettings
            LoggerHelper.WriteTelemetryMessage(TelemetryConstant.StartLogMessage);
            try
            {
                EnsureInitialized(catoolVersion, component);

                InitializeAndTrackEvent(TelemetryConstant.ToolName, catoolVersion, appDataEventName
                                                    , appSettings_, timeTaken);
                TrackKpiDataTelemetry(kpiEventName, kpiData);
            }
            catch (Exception ex) when (ex is ArgumentNullException or IOException)
            {
                LogHandlingHelper.ExceptionErrorHandling("Exception", "StartTelemetry()", ex, "");
                Logger.ErrorFormat("An error occurred: {0}", ex.Message);
                // KPI: "Error Rate by Stage" - pass component/version/project context so failures can be
                // filtered and grouped by stage (Scan/Create/Upload), project and tool version in Grafana.
                TrackException(ex, component, catoolVersion);
                environmentHelper.CallEnvironmentExit(-1);
            }
            finally
            {
                telemetry_?.Flush(); // Ensure telemetry is sent before application exits
            }
        }

        /// <summary>
        /// Initializes the underlying telemetry client if it hasn't been created yet. Safe to call multiple
        /// times (idempotent) and is used both by <see cref="StartTelemetry{T}"/> and by callers that need to
        /// report dependency health (<see cref="TrackDependencyCall"/>) earlier in the tool's execution,
        /// before the final app-data/KPI events are sent.
        /// </summary>
        /// <param name="catoolVersion">The CA tool version.</param>
        /// <param name="component">The standardized "Component" attribute identifying the SIT tool (e.g. Scan, Create, Upload).</param>
        public void EnsureInitialized(string catoolVersion, string component)
        {
            if (telemetry_ != null)
            {
                return;
            }

            telemetry_ = new SIT.Telemetry.Telemetry(TelemetryConstant.Type, new Dictionary<string, string>
            {
                { "ConnectionString", appSettings_?.Telemetry?.ApplicationInsightsConnectionString ?? string.Empty },
                { "Product", TelemetryConstant.Product },
                { "Application", TelemetryConstant.ToolName },
                { "Component", component },
                { "Environment", appSettings_?.Telemetry?.Environment ?? string.Empty },
                { "Version", catoolVersion ?? string.Empty },
                { "Organization", TelemetryConstant.Organization }
            });
        }

        /// <summary>
        /// Tracks the success/failure and latency of an outbound call to a dependent service
        /// (e.g. SW360, Artifactory). KPI: "Infrastructure Availability" - lets Grafana distinguish
        /// SIT-side failures from failures caused by an unavailable/slow dependency.
        /// </summary>
        /// <param name="serviceName">The dependent service name (e.g. "SW360", "Artifactory").</param>
        /// <param name="success">Whether the call succeeded.</param>
        /// <param name="duration">How long the call took.</param>
        /// <param name="component">The standardized "Component" attribute identifying the SIT tool (e.g. Scan, Create, Upload).</param>
        public void TrackDependencyCall(string serviceName, bool success, TimeSpan duration, string component)
        {
            if (telemetry_ == null)
            {
                // Telemetry not initialized (e.g. disabled) - no-op so callers don't need to guard themselves.
                return;
            }

            telemetry_.TrackCustomEvent(TelemetryConstant.DependencyHealthEvent, new Dictionary<string, string>
            {
                { "Service", serviceName ?? string.Empty },
                { "Success", success.ToString() },
                { "Duration Seconds", duration.TotalSeconds.ToString("0.##", CultureInfo.InvariantCulture) },
                { "Component", component ?? string.Empty },
                { "Run Id", runId_ }
            });
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
                // KPI: "Ecosystem Coverage" - normalize casing (e.g. "npm"/"Npm"/"NPM") so Grafana groups
                // all events for the same ecosystem together instead of fragmenting by raw input casing.
                { "Project Type", appSettings?.ProjectType?.ToUpperInvariant() ?? string.Empty },
                { "Hashed User ID", HashUtility.GetHashString(Environment.UserName) },
                { "Start Time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
                { "Time Taken For Completion", timeTaken.HasValue ? $"{timeTaken.Value.TotalSeconds:0.##} seconds" : string.Empty },
                // KPI: "Latency / Execution Time" - raw numeric seconds (no unit suffix) so Grafana/KQL can run
                // avg()/percentile() aggregations directly, instead of parsing the human-readable string above.
                { "Duration Seconds", timeTaken.HasValue ? timeTaken.Value.TotalSeconds.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty },
                // KPI: "Pipeline Completion Rate" / "Retry Rate" - lets Grafana join this event with the
                // corresponding events from the other SIT tools (Scan/Create/Upload) that share the same RunId.
                { "Run Id", runId_ }
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
            // KPI: "Pipeline Completion Rate" / "Retry Rate" - tag KPI events with the same RunId as the AppData
            // event so Scan/Create/Upload KPI numbers can be correlated back to a single pipeline execution.
            telemetryData["Run Id"] = runId_;

            telemetry_.TrackCustomEvent(eventName, telemetryData);
        }

        /// <summary>
        /// Tracks an exception with telemetry data.
        /// </summary>
        /// <param name="ex">The exception to track.</param>
        /// <param name="component">The standardized "Component" attribute identifying the SIT tool stage where the failure occurred.</param>
        /// <param name="catoolVersion">The CA tool version active when the failure occurred.</param>
        private void TrackException(Exception ex, string component = null, string catoolVersion = null)
        {
            var exceptionData = new Dictionary<string, string>
        {
            { "Error Time", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
            { "Stack Trace", ex.StackTrace },
            // KPI: "Error Rate by Stage" - which SIT stage (Scan/Create/Upload) the failure occurred in.
            { "Component", component ?? string.Empty },
            // KPI: "Error Rate by Stage" - CA tool version, to spot regressions introduced by a specific release.
            { "CA Tool Version", catoolVersion ?? string.Empty },
            // KPI: "Error Rate by Stage" - SW360 project context, to group/filter failures by project.
            { "SW360 Project Name", appSettings_?.SW360?.ProjectName ?? string.Empty },
            { "SW360 Project ID", appSettings_?.SW360?.ProjectID ?? string.Empty },
            // KPI: "Pipeline Completion Rate" / "Retry Rate" - correlate failures with the pipeline run that produced them.
            { "Run Id", runId_ }
        };

            telemetry_?.TrackException(ex, exceptionData);
        }

        #endregion
    }
}