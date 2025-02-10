using System.Diagnostics.CodeAnalysis;

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
        public const string StartLogMessage = "Telemetry for execution is now enabled and being tracked. You can disable it by using the command-line option --Telemetry=false in appsettings.";
    }
}