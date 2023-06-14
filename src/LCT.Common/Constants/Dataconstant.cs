// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Constants
{
    /// <summary>
    /// Common constants
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class Dataconstant
    {
        public const string Created = "Created";
        public const string NewlyCreated = "Newly Created";
        public const string Uploaded = "Uploaded";
        public const string Available = "Available";
        public const string NotCreated = "Not Created";
        public const string NotUploaded = "Not Uploaded";
        public const string NewClearing = "NEW_CLEARING";
        public const string NotAvailable = "Not Available";
        public const string AlreadyUploaded = "Already Uploaded";
        public const string NodeModulePathNotFound = "Node Module Path not Found";
        public const string DownloadUrlNotFound = "Component Download Url not Found!";
        public const string SourceUrlNotFound = "Source URL not found";
        public const string PackageUrlNotFound = "Package URL not found";
        public const string LinkedByCATool = "Linked by CA Tool";
        public const string ReleaseAttachmentComment = "Attached by CA Tool";
        public const char ForwardSlash = '/';
        public const string SourceURLSuffix = "/srcfiles?fileinfo=1";
        public const string DebianPackage = "pkg:deb/debian";
        public const string MavenPackage = "pkg:maven";
        public const string Cdx_ArtifactoryRepoUrl = "internal:siemens:clearing:repo-url";
        public const string Cdx_ProjectType = "internal:siemens:clearing:project-type";
        public const string Cdx_ClearingState = "internal:siemens:clearing:clearing-state";
        public const string Cdx_IsInternal = "internal:siemens:clearing:is-internal";
        public const string Cdx_ReleaseUrl = "internal:siemens:clearing:sw360:release-url";
        public const string Cdx_FossologyUrl = "internal:siemens:clearing:fossology:url";
        public const string Cdx_IsDevelopment = "internal:siemens:clearing:development";
        public const string Cdx_IdentifierType = "internal:siemens:clearing:identifier-type";
    }
}
