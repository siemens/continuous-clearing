// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    /// <summary>
    /// Reference Details
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ReferenceDetails
    {
        #region Fields
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the library name referenced.
        /// </summary>
        public string Library { get; set; }

        /// <summary>
        /// Gets or sets the library version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the reference is private.
        /// </summary>
        public bool Private { get; set; }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion

    }
}
