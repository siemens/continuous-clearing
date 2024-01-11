// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
//---------------------------------------------------------------------------------------------------------------------

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace LCT.ArtifactoryUploader.Model
{
    /// <summary>
    /// Uploader KPI Data
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class UploaderKpiData
    {
        [DisplayName(@"Components in Comparison BOM")]
        public int ComponentInComparisonBOM { get; set; }
        
        [DisplayName(@"Packages in Not Approved State")]
        public int ComponentNotApproved { get; set; }

        [DisplayName(@"Packages in Approved State")]
        public int PackagesToBeUploaded { get; set; }
        
        [DisplayName(@"Packages Copied to Siparty Repo")]
        public int PackagesUploadedToJfrog { get; set; }

        [DisplayName(@"Packages Not Copied to Siparty Repo")]
        public int PackagesNotUploadedToJfrog { get; set; }

        [DisplayName(@"Packages Not Existing in Repository")]
        public int PackagesNotExistingInRemoteCache { get; set; }

        [DisplayName(@"Packages Not Actioned Due To Error")]
        public int PackagesNotUploadedDueToError { get; set; }

        [DisplayName(@"Time taken by ComponentCreator")]
        public double TimeTakenByComponentCreator { get; set; }

        [DisplayName(@"Development Packages to be Moved to Siparty DevDep Repo")]
        public int DevPackagesToBeUploaded { get; set; }

        [DisplayName(@"Development Packages Moved to Siparty DevDep Repo")]
        public int DevPackagesUploaded { get; set; }

        [DisplayName(@"Development Packages Not Moved to Siparty DevDep Repo")]
        public int DevPackagesNotUploadedToJfrog { get; set; }

        [DisplayName(@"Internal Packages to be Moved")]
        public int InternalPackagesToBeUploaded { get; set; }

        [DisplayName(@"Internal Packages Moved to Repo")]
        public int InternalPackagesUploaded { get; set; }

        [DisplayName(@"Internal Packages Not Moved to Repo")]
        public int InternalPackagesNotUploadedToJfrog { get; set; }

    }
}
