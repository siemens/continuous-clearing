// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using NuGet.ProjectModel;
using NuGet.Versioning;
using System.Collections.Generic;

namespace LCT.PackageIdentifier.Interface
{
    /// <summary>
    /// FrameworkPackages interface
    /// </summary>
    public interface IFrameworkPackages
    {
        /// <summary>
        /// Gets framework packages from the specified lock file paths.
        /// </summary>
        /// <param name="lockFilePaths">The list of lock file paths to process.</param>
        /// <returns>A dictionary of framework packages organized by framework and package name with versions.</returns>
        Dictionary<string, Dictionary<string, NuGetVersion>> GetFrameworkPackages(List<string> lockFilePaths);

        /// <summary>
        /// Gets framework references from a lock file target.
        /// </summary>
        /// <param name="lockFile">The lock file to process.</param>
        /// <param name="target">The lock file target.</param>
        /// <returns>An array of framework reference names.</returns>
        string[] GetFrameworkReferences(LockFile lockFile, LockFileTarget target);
    }
}