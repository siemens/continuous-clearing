// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace SIT.APICommunications.Model
{
    /// <summary>
    /// Represents the release status information.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Releasestatus
    {
        #region Properties

        /// <summary>
        /// Gets or sets the SW360 releases information.
        /// </summary>
        public Sw360Releases sw360Releases { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the release exists.
        /// </summary>
        public bool isReleaseExist { get; set; }

        #endregion Properties
    }
}
