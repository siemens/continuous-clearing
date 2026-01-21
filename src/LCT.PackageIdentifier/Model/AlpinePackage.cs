// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

namespace LCT.PackageIdentifier.Model
{

    public class AlpinePackage
    {
        #region Properties
        /// <summary>
        /// Gets or sets the package name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the package version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the PURL identifier for the package.
        /// </summary>
        public string PurlID { get; set; }

        /// <summary>
        /// Gets or sets SPDX component details associated with the package.
        /// </summary>
        public SpdxComponentInfo SpdxComponentDetails { get; set; }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion
    }
}