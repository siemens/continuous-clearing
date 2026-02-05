// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;


namespace LCT.SW360PackageCreator.Model
{
    /// <summary>
    /// Represents Debian file information.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DebianFileInfo
    {
        #region Properties
        /// <summary>
        /// Gets or sets the name of the Debian file.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Gets or sets the archive name.
        /// </summary>
        public string archive_name { get; set; }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string path { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the file was first seen.
        /// </summary>
        public string first_seen { get; set; }
        #endregion
    }
}
