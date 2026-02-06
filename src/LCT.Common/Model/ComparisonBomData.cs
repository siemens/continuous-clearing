// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    /// <summary>
    /// Represents BOM data used for comparison operations with status tracking.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ComparisonBomData
    {
        #region Properties

        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the component group.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets the component version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the component external identifier.
        /// </summary>
        public string ComponentExternalId { get; set; }

        /// <summary>
        /// Gets or sets the release external identifier.
        /// </summary>
        public string ReleaseExternalId { get; set; }

        /// <summary>
        /// Gets or sets the package URL.
        /// </summary>
        public string PackageUrl { get; set; }

        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        public string SourceUrl { get; set; }

        /// <summary>
        /// Gets or sets the download URL.
        /// </summary>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the array of patch URLs.
        /// </summary>
        public string[] PatchURls { get; set; }

        /// <summary>
        /// Gets or sets the component status.
        /// </summary>
        public string ComponentStatus { get; set; }

        /// <summary>
        /// Gets or sets the release status.
        /// </summary>
        public string ReleaseStatus { get; set; }

        /// <summary>
        /// Gets or sets the approved status.
        /// </summary>
        public string ApprovedStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the component was created.
        /// </summary>
        public string IsComponentCreated { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the release was created.
        /// </summary>
        public string IsReleaseCreated { get; set; }

        /// <summary>
        /// Gets or sets the FOSSology upload status.
        /// </summary>
        public string FossologyUploadStatus { get; set; }

        /// <summary>
        /// Gets or sets the release attachment link.
        /// </summary>
        public string ReleaseAttachmentLink { get; set; }

        /// <summary>
        /// Gets or sets the release link.
        /// </summary>
        public string ReleaseLink { get; set; }

        /// <summary>
        /// Gets or sets the FOSSology link.
        /// </summary>
        public string FossologyLink { get; set; }

        /// <summary>
        /// Gets or sets the release identifier.
        /// </summary>
        public string ReleaseID { get; set; }

        /// <summary>
        /// Gets or sets the Alpine source information.
        /// </summary>
        public string AlpineSource { get; set; }

        /// <summary>
        /// Gets or sets the parent release name.
        /// </summary>
        public string ParentReleaseName { get; set; }

        /// <summary>
        /// Gets or sets the FOSSology upload identifier.
        /// </summary>
        public string FossologyUploadId { get; set; }

        /// <summary>
        /// Gets or sets the clearing state.
        /// </summary>
        public string ClearingState { get; set; }

        /// <summary>
        /// Gets or sets the user who created the release.
        /// </summary>
        public string ReleaseCreatedBy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the source attachment status is successful.
        /// </summary>
        public bool SourceAttachmentStatus { get; set; } = false;

        #endregion
    }
}
