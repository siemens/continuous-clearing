// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    /// <summary>
    /// MavenPackage constants
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MavenPackage
    {
        #region Properties
        /// <summary>
        /// Gets or sets the package identifier.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the package version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the package group identifier (groupId).
        /// </summary>
        public string GroupID { get; set; }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion
    }
}
