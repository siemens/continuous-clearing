// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    /// <summary>
    /// MultipleVersionValues model
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MultipleVersionValues
    {
        #region Properties

        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        public string ComponentName { get; set; }

        /// <summary>
        /// Gets or sets the component version.
        /// </summary>
        public string ComponentVersion { get; set; }

        /// <summary>
        /// Gets or sets the package where the component was found.
        /// </summary>
        public string PackageFoundIn { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents multiple versions found across different package managers.
    /// </summary>
    public class MultipleVersions
    {
        #region Properties

        /// <summary>
        /// Gets or sets the list of NPM packages with multiple versions.
        /// </summary>
        public List<MultipleVersionValues> Npm { get; set; }

        /// <summary>
        /// Gets or sets the list of NuGet packages with multiple versions.
        /// </summary>
        public List<MultipleVersionValues> Nuget { get; set; }

        /// <summary>
        /// Gets or sets the list of Conan packages with multiple versions.
        /// </summary>
        public List<MultipleVersionValues> Conan { get; set; }

        #endregion
    }
}
