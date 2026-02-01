// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    [ExcludeFromCodeCoverage]
    public class SpdxComponentInfo
    {
        #region Fields
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets a value indicating whether this component originates from an SPDX file.
        /// </summary>
        public bool SpdxComponent { get; set; } = false;

        /// <summary>
        /// Gets or sets the file path to the SPDX file that provided component information.
        /// </summary>
        public string SpdxFilePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this component is a development-only dependency.
        /// </summary>
        public bool DevComponent { get; set; } = false;
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion

    }
}
