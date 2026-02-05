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
        #region Properties
        /// <summary>
        /// Number of Debian components found in the input file.
        /// </summary>
        [DisplayName(@"Debian Components In Input File")]
        public int DebianComponents { get; set; }

        /// <summary>
        /// Number of components found in package-lock.json (or equivalent) input file.
        /// </summary>
        [DisplayName(@"Components In Input File")]
        public int ComponentsinPackageLockJsonFile { get; set; }

        /// <summary>
        /// Number of development (dev) dependent components.
        /// </summary>
        [DisplayName(@"Development Components")]
        public int DevDependentComponents { get; set; }

        /// <summary>
        /// Number of bundled components.
        /// </summary>
        [DisplayName(@"Bundled Components")]
        public int BundledComponents { get; set; }

        /// <summary>
        /// Number of duplicate components detected.
        /// </summary>
        [DisplayName(@"Duplicate Components")]
        public int DuplicateComponents { get; set; }

        /// <summary>
        /// Number of internal components.
        /// </summary>
        [DisplayName(@"Internal Components")]
        public int InternalComponents { get; set; }

        /// <summary>
        /// Number of packages present in third-party repositories.
        /// </summary>
        [DisplayName(@"Packages present in 3rd party repo(s)")]
        public int ThirdPartyRepoComponents { get; set; }

        /// <summary>
        /// Number of packages present in dev dependency repositories.
        /// </summary>
        [DisplayName(@"Packages present in devdep repo(s)")]
        public int DevdependencyComponents { get; set; }

        /// <summary>
        /// Number of packages present in release repositories.
        /// </summary>
        [DisplayName(@"Packages present in release repo(s)")]
        public int ReleaseRepoComponents { get; set; }

        /// <summary>
        /// Number of packages not present in official repositories.
        /// </summary>
        [DisplayName(@"Packages not present in official repo(s)")]
        public int UnofficialComponents { get; set; }

        /// <summary>
        /// Number of invalid components that were excluded.
        /// </summary>
        [DisplayName(@"Invalid Components Excluded")]
        public int ComponentsExcluded { get; set; }

        /// <summary>
        /// Number of components manually excluded via SW360.
        /// </summary>
        [DisplayName(@"Manually Excluded SW360")]
        public int ComponentsExcludedSW360 { get; set; }

        /// <summary>
        /// Number of components that include a SourceURL.
        /// </summary>
        [DisplayName(@"Components With SourceURL")]
        public int ComponentsWithSourceURL { get; set; }

        /// <summary>
        /// Number of components included in the comparison BOM.
        /// </summary>
        [DisplayName(@"Components in BoM")]
        public int ComponentsInComparisonBOM { get; set; }

        /// <summary>
        /// Time taken by the BOM creator, in seconds.
        /// </summary>
        [DisplayName(@"Time taken by BoM Creator")]
        public double TimeTakenByBomCreator { get; set; }

        /// <summary>
        /// Number of components added from an SBOM template file.
        /// </summary>
        [DisplayName(@"Components Added From SBoM Template")]
        public int ComponentsinSBOMTemplateFile { get; set; }

        /// <summary>
        /// Number of components updated from an SBOM template file.
        /// </summary>
        [DisplayName(@"Components overwritten from SBoM Template")]
        public int ComponentsUpdatedFromSBOMTemplateFile { get; set; }

        /// <summary>
        /// Number of unsupported components imported from an SPDX file as baseline entries.
        /// </summary>
        [DisplayName(@"Components from the SPDX imported as baseline entries")]
        public int UnsupportedComponentsFromSpdxFile { get; set; }

        /// <summary>
        /// Link to the project summary (if available).
        /// </summary>
        public string ProjectSummaryLink { get; set; }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion
    }
}
