// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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
        #region Properties
        /// <summary>
        /// Gets or sets the number of components read from the comparison BOM.
        /// </summary>
        [DisplayName(@"Components from BoM")]
        public int ComponentsReadFromComparisonBOM { get; set; }

        /// <summary>
        /// Gets or sets the number of components or releases created newly in SW360.
        /// </summary>
        [DisplayName(@"Releases created in SW360")]
        public int ComponentsOrReleasesCreatedNewlyInSw360 { get; set; }

        /// <summary>
        /// Gets or sets the number of components or releases already existing in SW360.
        /// </summary>
        [DisplayName(@"Releases exists in SW360")]
        public int ComponentsOrReleasesExistingInSw360 { get; set; }

        /// <summary>
        /// Gets or sets the number of releases without source download URL.
        /// </summary>
        [DisplayName(@"Releases Without source download URL")]
        public int ComponentsWithoutSourceDownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the number of releases with source download URL.
        /// </summary>
        [DisplayName(@"Releases with source download URL")]
        public int ComponentsWithSourceDownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the number of components or releases not created in SW360.
        /// </summary>
        [DisplayName(@"Releases not created in SW360")]
        public int ComponentsOrReleasesNotCreatedInSw360 { get; set; }

        /// <summary>
        /// Gets or sets the time taken by the component creator in seconds.
        /// </summary>
        [DisplayName(@"Time taken by ComponentCreator")]
        public double TimeTakenByComponentCreator { get; set; }

        /// <summary>
        /// Gets or sets the number of components without source and package URL.
        /// </summary>
        [DisplayName(@"Components without source and package URL")]
        public int ComponentsWithoutSourceAndPackageUrl { get; set; }

        /// <summary>
        /// Gets or sets the number of components without package URL.
        /// </summary>
        [DisplayName(@"Components without package URL")]
        public int ComponentsWithoutPackageUrl { get; set; }

        /// <summary>
        /// Gets or sets the number of components uploaded in FOSSology.
        /// </summary>
        [DisplayName(@"Components uploaded in FOSSology")]
        public int ComponentsUploadedInFossology { get; set; }

        /// <summary>
        /// Gets or sets the number of components not uploaded in FOSSology.
        /// </summary>
        [DisplayName(@"Components not uploaded in FOSSology")]
        public int ComponentsNotUploadedInFossology { get; set; }

        /// <summary>
        /// Gets or sets the total number of duplicate and invalid components.
        /// </summary>
        [DisplayName(@"Total Duplicate and InValid Components")]
        public int TotalDuplicateAndInValidComponents { get; set; }
        #endregion
    }
}
