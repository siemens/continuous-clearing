// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace LCT.SW360PackageCreator.Model
{
    /// <summary>
    /// Represents a Python package with its metadata.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class PythonPackage
    {
        #region Properties
        /// <summary>
        /// Gets or sets the name of the Python package.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version of the Python package.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the Package URL (PURL) identifier.
        /// </summary>
        public string PurlID { get; set; }

        /// <summary>
        /// Gets or sets the source URL of the Python package.
        /// </summary>
        public string SourceUrl { get; set; }

        /// <summary>
        /// Gets or sets the wheel distribution URL.
        /// </summary>
        public string WheelUrl { get; set; }
        #endregion
    }
}
