// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
//---------------------------------------------------------------------------------------------------------------------

// Ignore Spelling: LCT Kpi Repo Dev Sology Siparty Actioned

using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    [ExcludeFromCodeCoverage]
    public class KpiNames
    {

        public string ComponentsInInputFile { get; set; }

        public string DevelopmentComponents { get; set; }

        public string BundledComponents { get; set; }

        public string DuplicateComponents { get; set; }

        public string InternalComponents { get; set; }

        public string PackagesPresentIn3rdPartyRepo { get; set; }

        public string PackagesPresentInDevDepRepo { get; set; }

        public string PackagesPresentInReleaseRepo { get; set; }

        public string PackagesNotPresentInOfficialRepo { get; set; }

        public string InvalidComponentsExcluded { get; set; }

        public string ManuallyExcludedSw360 { get; set; }

        public string ComponentsWithSourceURL { get; set; }

        public string ComponentsInBOM { get; set; }

        public string ComponentsAddedFromSBOMTemplate { get; set; }

        public string ComponentsOverWrittenFromSBOMTemplate { get; set; }

        public string ComponentsFromTheSPDXImportedAsBaselineEntries { get; set; }
        public string ComponentsFromBOM { get; set; }

        public string ReleasesCreatedInSW360 { get; set; }

        public string ReleasesExistsInSW360 { get; set; }

        public string ReleasesWithoutSourceDownloadURL { get; set; }

        public string ReleasesWithSourceDownloadURL { get; set; }

        public string ReleasesNotCreatedInSW360 { get; set; }

        public string ComponentsWithoutSourceAndPackageURL { get; set; }

        public string ComponentsWithoutPackageURL { get; set; }

        public string ComponentsUploadedInFOSSology { get; set; }

        public string ComponentsNotUploadedInFOSSology { get; set; }

        public string TotalDuplicateAndInValidComponents { get; set; }
        public string PackagesInNotApprovedState { get; set; }

        public string PackagesInApprovedState { get; set; }

        public string PackagesCopiedToSipartyRepo { get; set; }

        public string PackagesNotCopiedToSipartyRepo { get; set; }

        public string PackagesNotExistingInRepository { get; set; }

        public string PackagesNotActionedDueToError { get; set; }

        public string PackagesCopiedToSipartyDevDepRepo { get; set; }

        public string PackagesNotCopiedToSipartyDevDepRepo { get; set; }

        public string PackagesMovedToRepo { get; set; }

        public string PackagesNotMovedToRepo { get; set; }

    }
}
