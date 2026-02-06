// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    /// <summary>
    /// Nuget Package
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class NugetPackage
    {
        #region Properties
        /// <summary>
        /// Package identifier (usually the package id or name).
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Package version.
        /// </summary>
        public string Version { get; set; }
        public List<string> Dependencies { get; set; }

        /// <summary>
        /// File path to the package or the source file where the package was declared.
        /// </summary>
        public string Filepath { get; set; }

        /// <summary>
        /// Indicates whether the package is a development dependency.
        /// </summary>
        public string IsDev { get; set; }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion
    }
}
