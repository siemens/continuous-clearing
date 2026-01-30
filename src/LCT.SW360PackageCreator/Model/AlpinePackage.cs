// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.SW360PackageCreator.Model
{
    /// <summary>
    /// Alpine Package Class
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AlpinePackage
    {
        #region Properties
        /// <summary>
        /// Gets or sets the name of the Alpine package.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version of the Alpine package.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the Package URL (PURL) identifier.
        /// </summary>
        public string PurlID { get; set; }

        /// <summary>
        /// Gets or sets the source URL of the Alpine package.
        /// </summary>
        public string SourceUrl { get; set; }

        /// <summary>
        /// Gets or sets the download URL of the Alpine package.
        /// </summary>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the source data specific to Alpine packages.
        /// </summary>
        public string SourceDataForAlpine { get; set; }
        #endregion
    }
}
