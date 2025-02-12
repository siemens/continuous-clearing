using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Common.Constants
{
    [ExcludeFromCodeCoverage]
    public class TelemetryConstant
    {
        public const string ToolName = "CATool";
        public const string PackageIdentifier = "PackageIdentifierExecution";
        public const string PackageCreator = "PackageCreatorExecution";
        public const string ArtifactoryUploader = "ArtifactoryUploaderExecution";
        public const string IdentifierKpiData = "IdentifierKpiDataTelemetry";
        public const string CreatorKpiData = "CreatorKpiDataTelemetry";
        public const string ArtifactoryUploaderKpiData = "UploaderKpiDataTelemetry";
        public const string Type = "ApplicationInsights";
        public const string StartLogMessage = "Telemetry for execution is now enabled and being tracked. You can disable it by using the command-line option --Telemetry:Enable false or via appsettings.";
    }
}
