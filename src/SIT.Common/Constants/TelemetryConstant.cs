// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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
        public const string ScanKpiData = "ScanKpiDataTelemetry";
        /// <summary>
        /// Telemetry key for SIT Create KPI data.
        /// </summary>
        public const string CreateKpiData = "CreateKpiDataTelemetry";
        /// <summary>
        /// Telemetry key for SIT Upload KPI data.
        /// </summary>
        public const string UploadKpiData = "UploadKpiDataTelemetry";
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
