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
        /// The name of the tool. Used as the standardized "Application" attribute.
        /// </summary>
        public const string ToolName = "CATool";
        /// <summary>
        /// The standardized "Product" attribute for all SIT telemetry events.
        /// </summary>
        public const string Product = "SBOM";
        /// <summary>
        /// The standardized "Organization" attribute for all SIT telemetry events.
        /// </summary>
        public const string Organization = "Siemens";
        /// <summary>
        /// The standardized "Component" attribute for SIT Scan telemetry events.
        /// </summary>
        public const string SITScanComponent = "SITScan";
        /// <summary>
        /// The standardized "Component" attribute for SIT Create telemetry events.
        /// </summary>
        public const string SITCreateComponent = "SITCreate";
        /// <summary>
        /// The standardized "Component" attribute for SIT Upload telemetry events.
        /// </summary>
        public const string SITUploadComponent = "SITUpload";
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
        /// Environment variable name used to share a single pipeline Run/Correlation ID across the separate
        /// SIT Scan, Create and Upload processes. KPI: enables "Pipeline Completion Rate" and "Retry/Re-run Rate"
        /// dashboards in Grafana by letting events from all stages of the same pipeline execution be joined on this ID.
        /// A CI/orchestrator script should set this variable once before invoking Scan/Create/Upload sequentially.
        /// </summary>
        public const string PipelineRunIdEnvironmentVariable = "SIT_PIPELINE_RUN_ID";
        /// <summary>
        /// Telemetry event name for dependency/infrastructure health checks (e.g. SW360, Artifactory).
        /// KPI: "Infrastructure Availability" - tracks success/failure and latency of outbound calls to
        /// dependent services so Grafana can surface whether failures originate from SIT or from a dependency.
        /// </summary>
        public const string DependencyHealthEvent = "DependencyHealth";

        /// <summary>
        /// Azure Pipelines predefined environment variable containing the Build ID of the running pipeline.
        /// </summary>
        public const string AzurePipelineBuildIdEnvironmentVariable = "BUILD_BUILDID";
        /// <summary>
        /// Azure Pipelines predefined environment variable containing the pipeline (build) definition name.
        /// </summary>
        public const string AzurePipelineDefinitionNameEnvironmentVariable = "BUILD_DEFINITIONNAME";
        /// <summary>
        /// Azure Pipelines predefined environment variable containing the Team Project name.
        /// </summary>
        public const string AzurePipelineTeamProjectEnvironmentVariable = "SYSTEM_TEAMPROJECT";
        /// <summary>
        /// Azure Pipelines predefined environment variable containing the source branch being built.
        /// </summary>
        public const string AzurePipelineSourceBranchEnvironmentVariable = "BUILD_SOURCEBRANCHNAME";
        /// <summary>
        /// Azure Pipelines predefined environment variable containing the repository name.
        /// </summary>
        public const string AzurePipelineRepositoryNameEnvironmentVariable = "BUILD_REPOSITORY_NAME";
        /// <summary>
        /// Azure Pipelines predefined environment variable containing the name of the agent running the job.
        /// </summary>
        public const string AzurePipelineAgentNameEnvironmentVariable = "AGENT_NAME";
        /// <summary>
        /// Azure Pipelines predefined environment variable identifying the stage name within the pipeline.
        /// </summary>
        public const string AzurePipelineStageNameEnvironmentVariable = "SYSTEM_STAGEDISPLAYNAME";
        /// <summary>
        /// Azure Pipelines predefined environment variable containing the organization/collection URL
        /// (e.g. "https://dev.azure.com/my-org/"). Combined with the Team Project and Build ID to build a
        /// direct link to the pipeline run, so Grafana can surface a "Pipeline URL" for failed runs.
        /// </summary>
        public const string AzurePipelineCollectionUriEnvironmentVariable = "SYSTEM_COLLECTIONURI";
        /// <summary>
        /// Azure Pipelines predefined environment variable indicating whether the run is executing inside
        /// Azure Pipelines ("True" when running in an Azure DevOps agent, unset otherwise).
        /// </summary>
        public const string AzurePipelineTfBuildEnvironmentVariable = "TF_BUILD";
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
