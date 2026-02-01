// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    /// <summary>
    /// Debian Package Class
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DebianPackage
    {
        #region Properties
        /// <summary>
        /// Package name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Package version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Package PURL identifier.
        /// </summary>
        public string PurlID { get; set; }

        /// <summary>
        /// Source URL for the Debian package.
        /// </summary>
        public string SourceUrl { get; set; }

        /// <summary>
        /// Patch URLs associated with the package.
        /// </summary>
        public string[] PatchURLs { get; set; }

        /// <summary>
        /// Download URL for the package artifact.
        /// </summary>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// Raw JSON text associated with this package, if available.
        /// </summary>
        public string JsonText { get; set; }

        /// <summary>
        /// Indicates whether a retry is required for fetching package metadata.
        /// </summary>
        public bool IsRetryRequired { get; set; }

        /// <summary>
        /// SPDX component details associated with this package.
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
