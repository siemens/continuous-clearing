// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
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
        [DisplayName(@"Debian Components in input file")]
        public int DebianComponents { get; set; }

        [DisplayName(@"Components in input file")]
        public int ComponentsinPackageLockJsonFile { get; set; }

        [DisplayName(@"Dev Dependent Components")]
        public int DevDependentComponents { get; set; }

        [DisplayName(@"Bundled Dependent Components")]
        public int BundledComponents { get; set; }

        [DisplayName(@"Total Duplicate Components")]
        public int DuplicateComponents { get; set; }

        [DisplayName(@"Internal Components Identified")]
        public int InternalComponents { get; set; }

        [DisplayName(@"Total Components Excluded")]
        public int ComponentsExcluded { get; set; }

        [DisplayName(@"Components With SourceURL")]
        public int ComponentsWithSourceURL { get; set; }

        [DisplayName(@"Components in Comparison BOM")]
        public int ComponentsInComparisonBOM { get; set; }

        [DisplayName(@"Time taken by BOM Creator")]
        public double TimeTakenByBomCreator { get; set; }

        public string ProjectSummaryLink { get; set; }
    }
}
