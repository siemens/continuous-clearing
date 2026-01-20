// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
//---------------------------------------------------------------------------------------------------------------------

// Ignore Spelling: LCT Kpi Repo Dev Sology Siparty Actioned

using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    /// <summary>
    /// Represents the names of various KPI metrics used for tracking components and packages.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class KpiNames
    {
        #region Properties

        /// <summary>
        /// Gets or sets the count of components in the input file.
        /// </summary>
        public string ComponentsInInputFile { get; set; }

        /// <summary>
        /// Gets or sets the count of development components.
        /// </summary>
        public string DevelopmentComponents { get; set; }

        /// <summary>
        /// Gets or sets the count of bundled components.
        /// </summary>
        public string BundledComponents { get; set; }

        /// <summary>
        /// Gets or sets the count of duplicate components.
        /// </summary>
        public string DuplicateComponents { get; set; }

        /// <summary>
        /// Gets or sets the count of internal components.
        /// </summary>
        public string InternalComponents { get; set; }

        /// <summary>
        /// Gets or sets the count of packages present in the 3rd party repository.
        /// </summary>
        public string PackagesPresentIn3rdPartyRepo { get; set; }

        /// <summary>
        /// Gets or sets the count of packages present in the development dependency repository.
        /// </summary>
        public string PackagesPresentInDevDepRepo { get; set; }

        /// <summary>
        /// Gets or sets the count of packages present in the release repository.
        /// </summary>
        public string PackagesPresentInReleaseRepo { get; set; }

        /// <summary>
        /// Gets or sets the count of packages not present in the official repository.
        /// </summary>
        public string PackagesNotPresentInOfficialRepo { get; set; }

        /// <summary>
        /// Gets or sets the count of invalid components excluded.
        /// </summary>
        public string InvalidComponentsExcluded { get; set; }

        /// <summary>
        /// Gets or sets the count of components manually excluded from SW360.
        /// </summary>
        public string ManuallyExcludedSw360 { get; set; }

        /// <summary>
        /// Gets or sets the count of components with source URL.
        /// </summary>
        public string ComponentsWithSourceURL { get; set; }

        /// <summary>
        /// Gets or sets the count of components in the BOM.
        /// </summary>
        public string ComponentsInBOM { get; set; }

        /// <summary>
        /// Gets or sets the count of components added from the SBOM template.
        /// </summary>
        public string ComponentsAddedFromSBOMTemplate { get; set; }

        /// <summary>
        /// Gets or sets the count of components overwritten from the SBOM template.
        /// </summary>
        public string ComponentsOverWrittenFromSBOMTemplate { get; set; }

        /// <summary>
        /// Gets or sets the count of components from the SPDX imported as baseline entries.
        /// </summary>
        public string ComponentsFromTheSPDXImportedAsBaselineEntries { get; set; }

        /// <summary>
        /// Gets or sets the count of components from the BOM.
        /// </summary>
        public string ComponentsFromBOM { get; set; }

        /// <summary>
        /// Gets or sets the count of releases created in SW360.
        /// </summary>
        public string ReleasesCreatedInSW360 { get; set; }

        /// <summary>
        /// Gets or sets the count of releases that exist in SW360.
        /// </summary>
        public string ReleasesExistsInSW360 { get; set; }

        /// <summary>
        /// Gets or sets the count of releases without source download URL.
        /// </summary>
        public string ReleasesWithoutSourceDownloadURL { get; set; }

        /// <summary>
        /// Gets or sets the count of releases with source download URL.
        /// </summary>
        public string ReleasesWithSourceDownloadURL { get; set; }

        /// <summary>
        /// Gets or sets the count of releases not created in SW360.
        /// </summary>
        public string ReleasesNotCreatedInSW360 { get; set; }

        /// <summary>
        /// Gets or sets the count of components without source and package URL.
        /// </summary>
        public string ComponentsWithoutSourceAndPackageURL { get; set; }

        /// <summary>
        /// Gets or sets the count of components without package URL.
        /// </summary>
        public string ComponentsWithoutPackageURL { get; set; }

        /// <summary>
        /// Gets or sets the count of components uploaded in FOSSology.
        /// </summary>
        public string ComponentsUploadedInFOSSology { get; set; }

        /// <summary>
        /// Gets or sets the count of components not uploaded in FOSSology.
        /// </summary>
        public string ComponentsNotUploadedInFOSSology { get; set; }

        /// <summary>
        /// Gets or sets the total count of duplicate and invalid components.
        /// </summary>
        public string TotalDuplicateAndInValidComponents { get; set; }

        /// <summary>
        /// Gets or sets the count of packages in not approved state.
        /// </summary>
        public string PackagesInNotApprovedState { get; set; }

        /// <summary>
        /// Gets or sets the count of packages in approved state.
        /// </summary>
        public string PackagesInApprovedState { get; set; }

        /// <summary>
        /// Gets or sets the count of packages copied to the Siparty repository.
        /// </summary>
        public string PackagesCopiedToSipartyRepo { get; set; }

        /// <summary>
        /// Gets or sets the count of packages not copied to the Siparty repository.
        /// </summary>
        public string PackagesNotCopiedToSipartyRepo { get; set; }

        /// <summary>
        /// Gets or sets the count of packages not existing in the repository.
        /// </summary>
        public string PackagesNotExistingInRepository { get; set; }

        /// <summary>
        /// Gets or sets the count of packages not actioned due to error.
        /// </summary>
        public string PackagesNotActionedDueToError { get; set; }

        /// <summary>
        /// Gets or sets the count of packages copied to the Siparty development dependency repository.
        /// </summary>
        public string PackagesCopiedToSipartyDevDepRepo { get; set; }

        /// <summary>
        /// Gets or sets the count of packages not copied to the Siparty development dependency repository.
        /// </summary>
        public string PackagesNotCopiedToSipartyDevDepRepo { get; set; }

        /// <summary>
        /// Gets or sets the count of packages moved to the repository.
        /// </summary>
        public string PackagesMovedToRepo { get; set; }

        /// <summary>
        /// Gets or sets the count of packages not moved to the repository.
        /// </summary>
        public string PackagesNotMovedToRepo { get; set; }

        #endregion
    }
}
