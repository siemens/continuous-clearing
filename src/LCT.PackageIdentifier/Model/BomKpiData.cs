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

        [DisplayName(@"Development Components")]
        public int DevDependentComponents { get; set; }

        [DisplayName(@"Bundled components")]
        public int BundledComponents { get; set; }

        [DisplayName(@"Duplicate Components")]
        public int DuplicateComponents { get; set; }

        [DisplayName(@"Internal components")]
        public int InternalComponents { get; set; }

        [DisplayName(@"Packages present in 3rd party repo(s)")]
        public int ThirdPartyRepoComponents { get; set; }

        [DisplayName(@"Packages present in devdep repo(s)")]
        public int DevdependencyComponents { get; set; }

        [DisplayName(@"Packages present in release repo(s)")]
        public int ReleaseRepoComponents { get; set; }

        [DisplayName(@"Packages not present in official repo(s)")]
        public int UnofficialComponents { get; set; }


        [DisplayName(@"Invalid Components Excluded")]
        public int ComponentsExcluded { get; set; }

        [DisplayName(@"Manually Excluded Sw360")]
        public int ComponentsExcludedSW360 { get; set; }

        [DisplayName(@"Components With SourceURL")]
        public int ComponentsWithSourceURL { get; set; }

        [DisplayName(@"Components in BoM")]
        public int ComponentsInComparisonBOM { get; set; }

        [DisplayName(@"Time taken by BOM Creator")]
        public double TimeTakenByBomCreator { get; set; }

        [DisplayName(@"Components Added From SBOM Template")]
        public int ComponentsinSBOMTemplateFile { get; set; }

        [DisplayName(@"Components overwritten from SBOM Template")]
        public int ComponentsUpdatedFromSBOMTemplateFile { get; set; }
        [DisplayName(@"Components from the SPDX imported as baseline entries")]
        public int UnsupportedComponentsFromSpdxFile { get; set; }

        public string ProjectSummaryLink { get; set; }
    }
}
