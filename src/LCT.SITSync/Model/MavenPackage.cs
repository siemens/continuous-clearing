// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.SITSync.Model
{
    /// <summary>
    /// MavenPackage constants
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MavenPackage
    {
        #region Properties
        /// <summary>
        /// Gets or sets the Maven package identifier.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the version of the Maven package.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets the Maven group identifier.
        /// </summary>
        public string GroupID { get; }
        #endregion
    }
}
