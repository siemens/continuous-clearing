// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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
        #region Fields
        /// <summary>
        /// File name for components without download URL.
        /// </summary>
        public const string ComponentsWithoutSrcFileName = "ComponentsWithoutDownloadURL.json";
        /// <summary>
        /// File name for components without source attachment in SW360.
        /// </summary>
        public const string SourceAttachmentNotAvailableInSw360 = "ComponentsWithoutSourceAttachmentInSW360.json";
        /// <summary>
        /// Default BOM file name.
        /// </summary>
        public const string BomFileName = "Bom.cdx.json";
        /// <summary>
        /// KPI data file name for BOM.
        /// </summary>
        public const string BomKpiDataFileName = "PackageIdentifierKpiData.json";
        /// <summary>
        /// KPI data file name for creator.
        /// </summary>
        public const string CreatorKpiDataFileName = "PacakageCreatorKpiData.json";
        /// <summary>
        /// KPI data file name for uploader.
        /// </summary>
        public const string UploaderKpiDataFileName = "FossUploaderKpiData.json";
        /// <summary>
        /// File extension for zip files.
        /// </summary>
        public const string ZipFileExtension = ".zip";
        /// <summary>
        /// File extension for tgz files.
        /// </summary>
        public const string TgzFileExtension = ".tgz";
        /// <summary>
        /// File extension for tar.gz files.
        /// </summary>
        public const string TargzFileExtension = ".tar.gz";
        /// <summary>
        /// File extension for NuGet package files.
        /// </summary>
        public const string NuspecFileExtension = ".nupkg";
        /// <summary>
        /// Log file name prefix for BOM.
        /// </summary>
        public const string BomLogFileNamePrefix = "PackageIdentifier_";
        /// <summary>
        /// Log file name prefix for creator.
        /// </summary>
        public const string CreatorLogFileNamePrefix = "PackageCreator_";
        /// <summary>
        /// Log file name prefix for uploader.
        /// </summary>
        public const string UploaderFileNamePrefix = "Fossology_";
        /// <summary>
        /// File name for package-lock.json.
        /// </summary>
        public const string PackageLockFileName = "package-lock.json";
        /// <summary>
        /// File name for packages.config.
        /// </summary>
        public const string PackageConfigFileName = "packages.config";
        /// <summary>
        /// File name for packages.lock.json.
        /// </summary>
        public const string PackageLockJonFileName = "packages.lock.json";
        /// <summary>
        /// Folder name for logs.
        /// </summary>
        public const string LogFolder = "Logs";
        /// <summary>
        /// Log file name for component creator.
        /// </summary>
        public const string ComponentCreatorLog = "PackageCreator.log";
        /// <summary>
        /// Log file name for BOM creator.
        /// </summary>
        public const string BomCreatorLog = "PackageIdentifier.log";
        /// <summary>
        /// Log file name for Fossology uploader.
        /// </summary>
        public const string FossologyUploaderLog = "FossologyUploader.log";
        /// <summary>
        /// Log file name for Artifactory uploader.
        /// </summary>
        public const string ArtifactoryUploaderLog = "ArtifactoryUploader.log";
        /// <summary>
        /// File extension for tar.xz files.
        /// </summary>
        public const string XzFileExtension = ".tar.xz";
        /// <summary>
        /// File extension for tar.bz2 files.
        /// </summary>
        public const string Bz2FileExtension = ".tar.bz2";
        /// <summary>
        /// File extension for orig.tar files.
        /// </summary>
        public const string OrigTarFileExtension = ".orig.tar.";
        /// <summary>
        /// File extension for debian.tar files.
        /// </summary>
        public const string DebianTarFileExtension = ".debian.tar.";
        /// <summary>
        /// File extension for debian diff files.
        /// </summary>
        public const string DebianFileExtension = ".diff.";
        /// <summary>
        /// File extension for debian combined patch files.
        /// </summary>
        public const string DebianCombinedPatchExtension = "-debian-combined.tar.bz2";
        /// <summary>
        /// File extension for DSC files.
        /// </summary>
        public const string DSCFileExtension = ".dsc";
        /// <summary>
        /// Directory path for container files.
        /// </summary>
        public static readonly string ContainerDir = Path.Combine(@"/app/opt/PatchedFiles");
        /// <summary>
        /// Docker image name.
        /// </summary>
        public const string DockerImage = "ghcr.io/siemens/continuous-clearing";
        /// <summary>
        /// Docker command tool path.
        /// </summary>
        public static readonly string DockerCMDTool = Path.Combine(@"/bin/bash");
        /// <summary>
        /// File name for package.json.
        /// </summary>
        public const string PackageJsonFileName = "package.json";
        /// <summary>
        /// File name for appSettings.json.
        /// </summary>
        public const string appSettingFileName = "appSettings.json";
        /// <summary>
        /// File extension for CycloneDX files.
        /// </summary>
        public const string CycloneDXFileExtension = ".cdx.json";
        /// <summary>
        /// File extension for SBOM template files.
        /// </summary>
        public const string SBOMTemplateFileExtension = "CATemplate.cdx.json";
        /// <summary>
        /// File name for NuGet asset files.
        /// </summary>
        public const string NugetAssetFile = "project.assets.json";
        /// <summary>
        /// File name for multiple versions file.
        /// </summary>
        public const string multipleversionsFileName = "Multipleversions.json";
        /// <summary>
        /// File name for Artifactory report not approved.
        /// </summary>
        public const string artifactoryReportNotApproved = "ReportNotApproved.json";
        /// <summary>
        /// Basic SBOM name.
        /// </summary>
        public const string basicSBOMName = "ContinuousClearing";
        /// <summary>
        /// File extensions for NuGet deployment type detection.
        /// </summary>
        public static readonly string[] Nuget_DeploymentType_DetectionExt = ["*.csproj", "*.Build.props"];
        /// <summary>
        /// Tags for NuGet deployment type detection.
        /// </summary>
        public static readonly string[] Nuget_DeploymentType_DetectionTags = ["SelfContained"];
        /// <summary>
        /// Backup key name.
        /// </summary>
        public const string backUpKey = "Backup";
        /// <summary>
        /// File extension for SPDX files.
        /// </summary>
        public const string SPDXFileExtension = ".spdx.sbom.json";
        /// <summary>
        /// File extension for Conan files.
        /// </summary>
        public const string ConanFileExtension = ".dep.json";
        /// <summary>
        /// File extension for Cargo files.
        /// </summary>
        public const string CargoFileExtension = "metadata.json";
        /// <summary>
        /// File extension for crate files.
        /// </summary>
        public const string CrateFileExtension = ".crate";
        public const string DependencyFileExtension = "cdx_dep.json";

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
