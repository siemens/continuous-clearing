// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.SW360PackageCreator.Model
{
    /// <summary>
    /// Debian Package Class
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DebianPackage
    {
        #region Properties
        /// <summary>
        /// Gets or sets the name of the Debian package.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version of the Debian package.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the Package URL (PURL) identifier.
        /// </summary>
        public string PurlID { get; set; }

        /// <summary>
        /// Gets or sets the source URL of the Debian package.
        /// </summary>
        public string SourceUrl { get; set; }

        /// <summary>
        /// Gets or sets the array of patch URLs.
        /// </summary>
        public string[] PatchURLs { get; set; }

        /// <summary>
        /// Gets or sets the download URL of the Debian package.
        /// </summary>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the JSON text representation.
        /// </summary>
        public string JsonText { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a retry is required.
        /// </summary>
        public bool IsRetryRequired { get; set; }
        #endregion
    }
}
