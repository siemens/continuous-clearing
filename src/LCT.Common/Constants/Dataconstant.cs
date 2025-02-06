// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
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
        private static Dictionary<string, string> purlids = new Dictionary<string, string>
         {
        {"NPM", "pkg:npm"},
        {"NUGET", "pkg:nuget"},
        {"DEBIAN", "pkg:deb/debian"},
        {"MAVEN", "pkg:maven"},
        {"POETRY", "pkg:pypi"},
        {"CONAN", "pkg:conan"},
        {"ALPINE", "pkg:apk/alpine"},
         };

        //Identified types
        public const string ManullayAdded = "ManuallyAdded";
        public const string Discovered = "Discovered";
        public const string TemplateAdded = "TemplateAdded";
        public const string TemplateUpdated = "TemplateUpdated";

        public const string Created = "Created";
        public const string NewlyCreated = "Newly Created";
        public const string Uploaded = "Uploaded";
        public const string Available = "Available";
        public const string NotCreated = "Not Created";
        public const string NotUploaded = "Not Uploaded";
        public const string NewClearing = "NEW_CLEARING";
        public const string NotAvailable = "Not Available";
        public const string NotConfigured = "Not Configured";
        public const string AlreadyUploaded = "Already Uploaded";
        public const string NodeModulePathNotFound = "Node Module Path not Found";
        public const string DownloadUrlNotFound = "Component Download Url not Found!";
        public const string SourceUrlNotFound = "Source URL not found";
        public const string PackageUrlNotFound = "Package URL not found";
        public const string PackageNameNotFoundInJfrog = "Package name not found in Jfrog";
        public const string JfrogRepoPathNotFound = "Jfrog repo path not found";
        public const string NotFoundInJFrog = "Not Found in JFrogRepo";
        public const string LinkedByCATool = "Linked by CA Tool";
        public const string LinkedByCAToolReleaseRelation = "UNKNOWN";
        public const string LinkedByCAToolReleaseRelationContained = "CONTAINED";
        public const string ReleaseAttachmentComment = "Attached by CA Tool";
        public const char ForwardSlash = '/';
        public const string SourceURLSuffix = "/srcfiles?fileinfo=1";
        public const string Cdx_ArtifactoryRepoName = "internal:siemens:clearing:jfrog-repo-name";
        public const string Cdx_ProjectType = "internal:siemens:clearing:project-type";
        public const string Cdx_ClearingState = "internal:siemens:clearing:clearing-state";
        public const string Cdx_IsInternal = "internal:siemens:clearing:is-internal";
        public const string Cdx_ReleaseUrl = "internal:siemens:clearing:sw360:release-url";
        public const string Cdx_FossologyUrl = "internal:siemens:clearing:fossology:url";
        public const string Cdx_IsDevelopment = "internal:siemens:clearing:development";
        public const string Cdx_IdentifierType = "internal:siemens:clearing:identifier-type";
        public const string AlpineSourceURLSuffix = "?ref_type=heads";
        public const string Cdx_JfrogRepoPath = "internal:siemens:clearing:jfrog-repo-path";
        public const string Cdx_Siemensfilename = "internal:siemens:clearing:siemens:filename";
        public const string Cdx_SiemensDirect = "internal:siemens:clearing:siemens:direct";
        public const string Cdx_ExcludeComponent = "internal:siemens:clearing:sw360:exclude";
        public const string ProductionFossologyURL = "automation.fossology";
        public const string StageFossologyURL = "stage.fossology";

        public static Dictionary<string, string> PurlCheck()
        {
            return purlids;
        }
    }
}
