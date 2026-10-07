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
