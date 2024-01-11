// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace LCT.SW360PackageCreator.Model
{
    /// <summary>
    /// Creator KPI Data
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CreatorKpiData
    {
        [DisplayName(@"Components read from Comparison BOM")]
        public int ComponentsReadFromComparisonBOM { get; set; }

        [DisplayName(@"Components or releases created newly in SW360")]
        public int ComponentsOrReleasesCreatedNewlyInSw360 { get; set; }

        [DisplayName(@"Components or releases exists in SW360")]
        public int ComponentsOrReleasesExistingInSw360 { get; set; }

        [DisplayName(@"Components without source download URL")]
        public int ComponentsWithoutSourceDownloadUrl { get; set; }

        [DisplayName(@"Components with source download URL")]
        public int ComponentsWithSourceDownloadUrl { get; set; }

        [DisplayName(@"Components or releases not created in SW360")]
        public int ComponentsOrReleasesNotCreatedInSw360 { get; set; }

        [DisplayName(@"Time taken by ComponentCreator")]
        public double TimeTakenByComponentCreator { get; set; }

        [DisplayName(@"Components without source and package URL")]
        public int ComponentsWithoutSourceAndPackageUrl { get; set; }

        [DisplayName(@"Components without package URL")]
        public int ComponentsWithoutPackageUrl { get; set; }

        [DisplayName(@"Components uploaded in FOSSology")]
        public int ComponentsUploadedInFossology { get; set; }

        [DisplayName(@"Components not uploaded in FOSSology")]
        public int ComponentsNotUploadedInFossology { get; set; }

        [DisplayName(@"Total Duplicate and InValid Components")]
        public int TotalDuplicateAndInValidComponents { get; set; }
    }
}
