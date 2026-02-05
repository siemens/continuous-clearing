// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// Represents arguments for upload operations.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class UploadArgs
    {
        #region Properties

        /// <summary>
        /// Gets or sets the package name.
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// Gets or sets the release name.
        /// </summary>
        public string ReleaseName { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        public string Version { get; set; }

        #endregion Properties
    }
}
