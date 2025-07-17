// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    /// <summary>
    /// BomKpiData model
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class BomKpiData
    {
        [DisplayName(@"Debian Components In Input File")]
        public int DebianComponents { get; set; }

        [DisplayName(@"Components In Input File")]
        public int ComponentsinPackageLockJsonFile { get; set; }

        [DisplayName(@"Dev Dependent Components")]
        public int DevDependentComponents { get; set; }

        [DisplayName(@"Bundled Dependent Components")]
        public int BundledComponents { get; set; }

        [DisplayName(@"Total Duplicate Components")]
        public int DuplicateComponents { get; set; }

        [DisplayName(@"Internal Components Identified")]
        public int InternalComponents { get; set; }

        [DisplayName(@"Components already present in 3rd party repo(s) ")]
        public int ThirdPartyRepoComponents { get; set; }

        [DisplayName(@"Components already present in devdep repo(s)")]
        public int DevdependencyComponents { get; set; }

        [DisplayName(@"Components already present in release repo(s)")]
        public int ReleaseRepoComponents { get; set; }

        [DisplayName(@"Components not from official repo(s)")]
        public int UnofficialComponents { get; set; }


        [DisplayName(@"Total InvalidComponents Excluded")]
        public int ComponentsExcluded { get; set; }

        [DisplayName(@"Total Components Excluded SW360")]
        public int ComponentsExcludedSW360 { get; set; }

        [DisplayName(@"Components With SourceURL")]
        public int ComponentsWithSourceURL { get; set; }

        [DisplayName(@"Components In Comparison BOM")]
        public int ComponentsInComparisonBOM { get; set; }

        [DisplayName(@"Time taken by BOM Creator")]
        public double TimeTakenByBomCreator { get; set; }

        [DisplayName(@"Components Added From SBOM Template")]
        public int ComponentsinSBOMTemplateFile { get; set; }

        [DisplayName(@"Components Updated From SBOM Template")]
        public int ComponentsUpdatedFromSBOMTemplateFile { get; set; }
        [DisplayName(@"Unsupported Components Identified From Spdx")]
        public int UnsupportedComponentsFromSpdxFile { get; set; }

        public string ProjectSummaryLink { get; set; }
    }
}
