// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    /// <summary>
    /// ComparisonBomData model
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ComparisonBomData
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public string Version { get; set; }
        public string ComponentExternalId { get; set; }
        public string ReleaseExternalId { get; set; }
        public string PackageUrl { get; set; }
        public string SourceUrl { get; set; }
        public string DownloadUrl { get; set; }
        public string[] PatchURls { get; set; }
        public string ComponentStatus { get; set; }
        public string ReleaseStatus { get; set; }
        public string ApprovedStatus { get; set; }
        public string IsComponentCreated { get; set; }
        public string IsReleaseCreated { get; set; }
        public string FossologyUploadStatus { get; set; }
        public string ReleaseAttachmentLink { get; set; }
        public string ReleaseLink { get; set; }
        public string FossologyLink { get; set; }
        public string ReleaseID { get; set; }
        public string AlpineSource { get; set; }
        public string ParentReleaseName { get; set; }
        public string FossologyUploadId { get; set; }
    }
}
