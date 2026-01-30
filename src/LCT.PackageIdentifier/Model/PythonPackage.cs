// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT

// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    [ExcludeFromCodeCoverage]
    public class PythonPackage
    {
        #region Properties
        /// <summary>
        /// Package URL identifier (PURL) for the Python package.
        /// </summary>
        public string PurlID { get; set; }

        /// <summary>
        /// Package name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Package version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Indicates whether the package is a development dependency.
        /// </summary>
        public bool Isdevdependent { get; set; }

        /// <summary>
        /// The detected type/source of the package (for example "pip" or "poetry").
        /// </summary>
        public string FoundType { get; set; }

        /// <summary>
        /// File path where the package was found or declared.
        /// </summary>
        public string Filepath { get; set; }

        /// <summary>
        /// SPDX component information associated with this package.
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
