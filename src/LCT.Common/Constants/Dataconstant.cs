// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Constants
{
    /// <summary>
    /// Common constants
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class Dataconstant
    {
        #region Fields
        /// <summary>
        /// Dictionary of package type to purl id.
        /// </summary>
        private static Dictionary<string, string> purlids = new Dictionary<string, string>
         {
        {"NPM", "pkg:npm"},
        {"NUGET", "pkg:nuget"},
        {"DEBIAN", "pkg:deb/debian"},
        {"MAVEN", "pkg:maven"},
        {"POETRY", "pkg:pypi"},
        {"CONAN", "pkg:conan"},
        {"ALPINE", "pkg:apk/alpine"},
        {"CARGO", "pkg:cargo"},
         };
        /// <summary>
        /// Identified type: Manually added.
        /// </summary>
        public const string ManullayAdded = "ManuallyAdded";
        /// <summary>
        /// Identified type: Discovered.
        /// </summary>
        public const string Discovered = "Discovered";
        /// <summary>
        /// Identified type: Template added.
        /// </summary>
        public const string TemplateAdded = "TemplateAdded";
        /// <summary>
        /// Identified type: Template updated.
        /// </summary>
        public const string TemplateUpdated = "TemplateUpdated";
        /// <summary>
        /// State: Created.
        /// </summary>
        public const string Created = "Created";
        /// <summary>
        /// State: Newly created.
        /// </summary>
        public const string NewlyCreated = "Newly Created";
        /// <summary>
        /// State: Uploaded.
        /// </summary>
        public const string Uploaded = "Uploaded";
        /// <summary>
        /// State: Available.
        /// </summary>
        public const string Available = "Available";
        /// <summary>
        /// State: Not created.
        /// </summary>
        public const string NotCreated = "Not Created";
        /// <summary>
        /// State: Not uploaded.
        /// </summary>
        public const string NotUploaded = "Not Uploaded";
        /// <summary>
        /// State: New clearing.
        /// </summary>
        public const string NewClearing = "NEW_CLEARING";
        /// <summary>
        /// State: Approved.
        /// </summary>
        public const string Approved = "APPROVED";
        /// <summary>
        /// State: Not available.
        /// </summary>
        public const string NotAvailable = "Not Available";
        /// <summary>
        /// State: Not configured.
        /// </summary>
        public const string NotConfigured = "Not Configured";
        /// <summary>
        /// State: Already uploaded.
        /// </summary>
        public const string AlreadyUploaded = "Already Uploaded";
        /// <summary>
        /// Error: Node module path not found.
        /// </summary>
        public const string NodeModulePathNotFound = "Node Module Path not Found";
        /// <summary>
        /// Error: Component download URL not found.
        /// </summary>
        public const string DownloadUrlNotFound = "Component Download Url not Found!";
        /// <summary>
        /// Error: Source URL not found.
        /// </summary>
        public const string SourceUrlNotFound = "Source URL not found";
        /// <summary>
        /// Error: Package URL not found.
        /// </summary>
        public const string PackageUrlNotFound = "Package URL not found";
        /// <summary>
        /// Error: Package name not found in JFrog.
        /// </summary>
        public const string PackageNameNotFoundInJfrog = "Package name not found in Jfrog";
        /// <summary>
        /// Error: JFrog repo path not found.
        /// </summary>
        public const string JfrogRepoPathNotFound = "Jfrog repo path not found";
        /// <summary>
        /// Error: Not found in JFrog repo.
        /// </summary>
        public const string NotFoundInJFrog = "Not Found in JFrogRepo";
        /// <summary>
        /// Linked by CA Tool.
        /// </summary>
        public const string LinkedByCATool = "Linked by CA Tool";
        /// <summary>
        /// Linked by CA Tool release relation: UNKNOWN.
        /// </summary>
        public const string LinkedByCAToolReleaseRelation = "UNKNOWN";
        /// <summary>
        /// Linked by CA Tool release relation: CONTAINED.
        /// </summary>
        public const string LinkedByCAToolReleaseRelationContained = "CONTAINED";
        /// <summary>
        /// Release attachment comment.
        /// </summary>
        public const string ReleaseAttachmentComment = "Attached by CA Tool";
        /// <summary>
        /// Forward slash character.
        /// </summary>
        public const char ForwardSlash = '/';
        /// <summary>
        /// Suffix for source URL.
        /// </summary>
        public const string SourceURLSuffix = "/srcfiles?fileinfo=1";
        /// <summary>
        /// CycloneDX Artifactory repo name key.
        /// </summary>
        public const string Cdx_ArtifactoryRepoName = "internal:siemens:clearing:jfrog-repo-name";
        /// <summary>
        /// CycloneDX project type key.
        /// </summary>
        public const string Cdx_ProjectType = "internal:siemens:clearing:project-type";
        /// <summary>
        /// CycloneDX clearing state key.
        /// </summary>
        public const string Cdx_ClearingState = "internal:siemens:clearing:clearing-state";
        /// <summary>
        /// CycloneDX is internal key.
        /// </summary>
        public const string Cdx_IsInternal = "internal:siemens:clearing:is-internal";
        /// <summary>
        /// CycloneDX release URL key.
        /// </summary>
        public const string Cdx_ReleaseUrl = "internal:siemens:clearing:sw360:release-url";
        /// <summary>
        /// CycloneDX Fossology URL key.
        /// </summary>
        public const string Cdx_FossologyUrl = "internal:siemens:clearing:fossology:url";
        /// <summary>
        /// CycloneDX is development key.
        /// </summary>
        public const string Cdx_IsDevelopment = "internal:siemens:clearing:development";
        /// <summary>
        /// CycloneDX identifier type key.
        /// </summary>
        public const string Cdx_IdentifierType = "internal:siemens:clearing:identifier-type";
        /// <summary>
        /// Suffix for Alpine source URL.
        /// </summary>
        public const string AlpineSourceURLSuffix = "?ref_type=heads";
        /// <summary>
        /// CycloneDX JFrog repo path key.
        /// </summary>
        public const string Cdx_JfrogRepoPath = "internal:siemens:clearing:jfrog-repo-path";
        /// <summary>
        /// CycloneDX Siemens filename key.
        /// </summary>
        public const string Cdx_Siemensfilename = "internal:siemens:clearing:siemens:filename";
        /// <summary>
        /// CycloneDX Siemens direct key.
        /// </summary>
        public const string Cdx_SiemensDirect = "internal:siemens:clearing:siemens:direct";
        /// <summary>
        /// CycloneDX exclude component key.
        /// </summary>
        public const string Cdx_ExcludeComponent = "internal:siemens:clearing:sw360:exclude";
        /// <summary>
        /// Production Fossology URL.
        /// </summary>
        public const string ProductionFossologyURL = "automation.fossology";
        /// <summary>
        /// Stage Fossology URL.
        /// </summary>
        public const string StageFossologyURL = "stage.fossology";
        /// <summary>
        /// Scan available state.
        /// </summary>
        public const string ScanAvailableState = "SCAN_AVAILABLE";
        /// <summary>
        /// Sent to clearing state.
        /// </summary>
        public const string SentToClearingState = "SENT_TO_CLEARING_TOOL";
        /// <summary>
        /// Type jar suffix.
        /// </summary>
        public const string TypeJarSuffix = "?type=jar";
        /// <summary>
        /// GitHub URL for continuous-clearing.
        /// </summary>
        public const string GithubUrl = "https://github.com/siemens/continuous-clearing";
        /// <summary>
        /// Standard SBOM URL.
        /// </summary>
        public const string StandardSbomUrl = "https://sbom.siemens.io/";
        /// <summary>
        /// SBOM spec version string.
        /// </summary>
        public const string SbomSpecVersionString = "1.6";
        /// <summary>
        /// Moderation request message.
        /// </summary>
        public const string ModerationRequestMessage = "Moderation request is created";
        /// <summary>
        /// CycloneDX SPDX file name key.
        /// </summary>
        public const string Cdx_SpdxFileName = "internal:siemens:clearing:spdx-file-name";
        /// <summary>
        /// SPDX import string.
        /// </summary>
        public const string SpdxImport = "SPDXImport";
        /// <summary>
        /// Identifier string.
        /// </summary>
        public const string Identifier = "Identifier";
        /// <summary>
        /// Creator string.
        /// </summary>
        public const string Creator = "Creator";
        /// <summary>
        /// Uploader string.
        /// </summary>
        public const string Uploader = "Uploader";
        #endregion

        #region Properties
        // No properties present.
        #endregion

        #region Constructors
        // No constructors present.
        #endregion

        #region Methods
        /// <summary>
        /// Gets the dictionary of package type to purl id.
        /// </summary>
        /// <returns>The dictionary mapping package type to purl id.</returns>
        public static Dictionary<string, string> PurlCheck()
        {
            return purlids;
        }
        #endregion

        #region Events
        // No events present.
        #endregion
    }
}
