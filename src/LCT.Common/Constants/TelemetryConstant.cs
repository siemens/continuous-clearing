// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Constants
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
        /// Telemetry key for package identifier execution.
        /// </summary>
        public const string PackageIdentifier = "PackageIdentifierExecution";
        /// <summary>
        /// Telemetry key for package creator execution.
        /// </summary>
        public const string PackageCreator = "PackageCreatorExecution";
        /// <summary>
        /// Telemetry key for Artifactory uploader execution.
        /// </summary>
        public const string ArtifactoryUploader = "ArtifactoryUploaderExecution";
        /// <summary>
        /// Telemetry key for identifier KPI data.
        /// </summary>
        public const string IdentifierKpiData = "IdentifierKpiDataTelemetry";
        /// <summary>
        /// Telemetry key for creator KPI data.
        /// </summary>
        public const string CreatorKpiData = "CreatorKpiDataTelemetry";
        /// <summary>
        /// Telemetry key for Artifactory uploader KPI data.
        /// </summary>
        public const string ArtifactoryUploaderKpiData = "UploaderKpiDataTelemetry";
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
