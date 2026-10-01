// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using System.Diagnostics.CodeAnalysis;

namespace SIT.Common.Constants
{
    [ExcludeFromCodeCoverage]
    public static class TelemetryConstant
    {
        #region Fields
        /// <summary>
        /// The name of the tool.
        /// </summary>
        public const string ToolName = "CATool";
        /// <summary>
        /// Telemetry key for SIT Scan execution.
        /// </summary>
        public const string SITScan = "SITScanExecution";
        /// <summary>
        /// Telemetry key for SIT Create execution.
        /// </summary>
        public const string SITCreate = "SITCreateExecution";
        /// <summary>
        /// Telemetry key for SIT Upload execution.
        /// </summary>
        public const string SITUpload = "SITUploadExecution";
        /// <summary>
        /// Telemetry key for SIT Scan KPI data.
        /// </summary>
        public const string ScanKpiData = "SITScanKpiDataTelemetry";
        /// <summary>
        /// Telemetry key for SIT Create KPI data.
        /// </summary>
        public const string CreateKpiData = "SITCreateKpiDataTelemetry";
        /// <summary>
        /// Telemetry key for SIT Upload KPI data.
        /// </summary>
        public const string UploadKpiData = "SITUploadKpiDataTelemetry";
        /// <summary>
        /// Telemetry key for SIT Create application/context data.
        /// </summary>
        public const string CreateAppData = "SITCreateAppData";
        /// <summary>
        /// Telemetry key for SIT Scan application/context data.
        /// </summary>
        public const string ScanAppData = "SITScanAppData";
        /// <summary>
        /// Telemetry key for SIT Upload application/context data.
        /// </summary>
        public const string UploadAppData = "SITUploadAppData";
        /// <summary>
        /// The type of telemetry (e.g., ApplicationInsights).
        /// </summary>
        public const string Type = "ApplicationInsights";
        /// <summary>
        /// The log message displayed when telemetry tracking starts.
        /// </summary>
        public const string StartLogMessage = "Telemetry tracking is now active for this execution. To turn off telemetry, use the command-line option --Telemetry:Enable false or adjust the settings in your appsettings file.";

        /// <summary>
        /// Environment variable used to correlate all stages of the same pipeline run.
        /// </summary>
        public const string RunIdEnvironmentVariable = "SIT_RUN_ID";

        #region Stage Names
        public const string StageScan = "scan";
        public const string StageCreate = "create";
        public const string StageUpload = "upload";
        public const string StageBuild = "build";
        public const string StageValidate = "validate";
        public const string StageArchive = "archive";
        #endregion

        #region Common Attribute Keys
        public const string AttrStage = "sit.stage";
        public const string AttrProjectName = "sit.project.name";
        public const string AttrProjectId = "sit.project.id";
        public const string AttrRunId = "sit.run.id";
        public const string AttrToolVersion = "sit.tool.version";
        public const string AttrStatus = "sit.status";
        public const string AttrPackageType = "sit.package_type";
        public const string AttrUserHash = "sit.user.hash";
        #endregion

        #region Status Values
        public const string StatusSuccess = "success";
        public const string StatusFailure = "failure";
        #endregion

        /// <summary>
        /// Metric name for stage execution duration, tagged with sit.stage.
        /// </summary>
        public const string MetricStageDuration = "sit.stage.duration_seconds";

        /// <summary>
        /// Metric name for stage execution count, tagged with sit.stage and sit.status.
        /// </summary>
        public const string MetricStageExecutionCount = "sit.stage.execution_count";
        #endregion

        #region Properties
        // No properties present.
        #endregion

        #region Constructors
        // No constructors present.
        #endregion

        #region Methods
        // No methods present.
        #endregion

        #region Events
        // No events present.
        #endregion
    }
}
