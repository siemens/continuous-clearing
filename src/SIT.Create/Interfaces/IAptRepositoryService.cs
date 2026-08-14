// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using SIT.Common.Model;
using SIT.Create.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Create.Interfaces
{
    /// <summary>
    /// Resolves the source package of a binary package by reading the index files of APT archives.
    /// </summary>
    public interface IAptRepositoryService
    {
        /// <summary>
        /// Searches the given APT archives for the source package a binary package was built from.
        /// </summary>
        /// <param name="repositories">the APT archives to search, in order of precedence</param>
        /// <param name="binaryName">name of the installed binary package</param>
        /// <param name="binaryVersion">version of the installed binary package</param>
        /// <param name="suiteCandidates">suites to search when a repository does not configure any</param>
        /// <returns>the source package details, or null when none of the archives provides the package</returns>
        Task<AptSourceResolution> ResolveSourcePackageAsync(IReadOnlyList<AptRepository> repositories,
                                                            string binaryName,
                                                            string binaryVersion,
                                                            IReadOnlyList<string> suiteCandidates);
    }
}
