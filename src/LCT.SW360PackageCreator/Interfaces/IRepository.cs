// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.SW360PackageCreator.Interfaces
{
    /// <summary>
    /// The interface IRepository
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// Identifies the github repository URL to download the source
        /// </summary>
        /// <param name="url"></param>
        /// <param name="componentName"></param>
        /// <returns>string</returns>
        string IdentifyRepoURLForGit(string url, string componentName);

        /// <summary>
        /// Forms the git clone url from the given url
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="componentName"></param>
        /// <returns>string</returns>
        string FormGitCloneUrl(string url, string componentName, string version);
    }
}
