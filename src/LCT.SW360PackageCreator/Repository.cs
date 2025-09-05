// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Common.Constants;
using LCT.SW360PackageCreator.Interfaces;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// The Repository class
    /// </summary>
    public class Repository : IRepository
    {

        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly List<string> m_RepoUrlList = new List<string>()
            {
                "github.com",
                "gitlab.com",
                "bitbucket.org"
            };

        /// <summary>
        /// Identifies the github repository URL to download the source
        /// </summary>
        /// <param name="url"></param>
        /// <param name="componentName"></param>
        /// <returns>string</returns>
        string IRepository.IdentifyRepoURLForGit(string url, string componentName)
        {
            Logger.Debug($"Repository.IdentifyRepoURLForGitHub():Start");
            string downloadUrl = string.Empty;
            string repoName = GetRepoName(url);

            if (string.IsNullOrEmpty(repoName))
            {
                return downloadUrl;
            }

            try
            {
                int startIndexOfGitHub = url.IndexOf(repoName);
                downloadUrl = url.Remove(0, startIndexOfGitHub);
                downloadUrl = downloadUrl.Replace(".git", "/", StringComparison.OrdinalIgnoreCase);
                downloadUrl = $"https://{downloadUrl}";
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Environment.ExitCode = -1;
                Logger.Error($"Repository.IdentifyRepoURLForGitHub():{componentName}-{url}", ex);
            }

            Logger.Debug($"Repository.IdentifyRepoURLForGitHub():End");
            return downloadUrl;
        }

        public string FormGitCloneUrl(string url, string componentName, string version)
        {
            Logger.Debug($"FormGitCloneUrl():Start");
            string downloadUrl = Dataconstant.DownloadUrlNotFound;

            if (string.IsNullOrEmpty(url) || url == Dataconstant.SourceUrlNotFound)
            {
                return downloadUrl;
            }

            string cloneUrl = url.TrimEnd('/');
            downloadUrl = $"{cloneUrl}.git";

            Logger.Debug($"FormGitCloneUrl():End");
            return downloadUrl;
        }

        private string GetRepoName(string url)
        {
            string repoUrl = string.Empty;
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            repoUrl = (from string repoUrl2 in m_RepoUrlList
                       where url.Contains(repoUrl2)
                       select repoUrl2).FirstOrDefault();


            return repoUrl;
        }
    }
}
