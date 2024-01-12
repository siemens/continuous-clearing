// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace LCT.Common.Constants
{
    /// <summary>
    /// The file constants 
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class FileConstant
    {
        public const string ComponentsWithoutSrcFileName = "ComponentsWithoutDownloadURL.json";
        public const string SourceAttachmentNotAvailableInSw360 = "ComponentsWithoutSourceAttachmentInSW360.json";
        public const string BomFileName = "Bom.cdx.json";
        public const string BomKpiDataFileName = "PackageIdentifierKpiData.json";
        public const string CreatorKpiDataFileName = "PacakageCreatorKpiData.json";
        public const string UploaderKpiDataFileName = "FossUploaderKpiData.json";
        public const string ZipFileExtension = ".zip";
        public const string TgzFileExtension = ".tgz";
        public const string TargzFileExtension = ".tar.gz";
        public const string NuspecFileExtension = ".nupkg";
        public const string BomLogFileNamePrefix = "PackageIdentifier_";
        public const string CreatorLogFileNamePrefix = "PackageCreator_";
        public const string UploaderFileNamePrefix = "Fossology_";
        public const string PackageLockFileName = "package-lock.json";
        public const string PackageConfigFileName = "packages.config";
        public const string PackageLockJonFileName = "packages.lock.json";
        public const string LogFolder = "..\\Logs";
        public const string ComponentCreatorLog = "PacakgeCreator.log";
        public const string BomCreatorLog = "PacakgeIdentifier.log";
        public const string FossologyUploaderLog = "FossologyUploader.log";
        public const string ArtifactoryUploaderLog = "ArtifactoryUploader.log";
        public const string XzFileExtension = ".tar.xz";
        public const string Bz2FileExtension = ".tar.bz2";
        public const string OrigTarFileExtension = ".orig.tar.";
        public const string DebianTarFileExtension = ".debian.tar.";
        public const string DebianFileExtension = ".diff.";
        public const string DebianCombinedPatchExtension = "-debian-combined.tar.bz2";
        public const string DSCFileExtension = ".dsc";
        public static readonly string ContainerDir = Path.Combine(@"/app/opt/PatchedFiles");
        public const string DockerImage = "ghcr.io/siemens/continuous-clearing";
        public static readonly string DockerCMDTool = Path.Combine(@"/bin/bash");
        public const string appSettingFileName = "appSettings.json";
        public const string CycloneDXFileExtension = ".cdx.json"; 
        public const string SBOMTemplateFileExtension = "CATemplate.cdx.json";
        public const string NugetAssetFile = "project.assets.json";
    }
}
