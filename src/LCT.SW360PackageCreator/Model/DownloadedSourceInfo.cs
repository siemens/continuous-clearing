// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.SW360PackageCreator.Model
{
    /// <summary>
    /// Downloaded Source Info
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DownloadedSourceInfo
    {
        #region Properties
        /// <summary>
        /// Gets or sets the name of the downloaded source.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version of the downloaded source.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the source repository URL.
        /// </summary>
        public string SourceRepoUrl { get; set; }

        /// <summary>
        /// Gets or sets the path where the source was downloaded.
        /// </summary>
        public string DownloadedPath { get; set; }

        /// <summary>
        /// Gets or sets the tagged version of the source.
        /// </summary>
        public string TaggedVersion { get; set; }
        #endregion
    }
}
