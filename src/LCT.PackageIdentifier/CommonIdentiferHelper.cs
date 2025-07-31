// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LCT.PackageIdentifier
{
    public static class CommonIdentiferHelper
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
        public static string GetRepodetailsFromPerticularOrder(List<AqlResult> aqlResults)
        {
            Logger.Debug("GetRepodetailsFromPerticularOrder(): Starting repository details retrieval from AQL results.");

            if (aqlResults == null)
            {
                Logger.Debug("GetRepodetailsFromPerticularOrder(): No repositories identified from aqlresult. Returning 'Not Found in Repo'.");
                return NotFoundInRepo;
            }

            Logger.Debug($"GetRepodetailsFromPerticularOrder(): Total repositories identified from AQL result: {aqlResults.Count}");
            var repoKeywords = new[] { "release", "devdep", "dev" };
            string repo = FindRepositoryByKeywords(aqlResults, repoKeywords);

            if (repo != null)
            {
                Logger.Debug($"GetRepodetailsFromPerticularOrder(): Found repository: {repo}");
                return repo;
            }
            repo = aqlResults.FirstOrDefault()?.Repo ?? NotFoundInRepo;
            Logger.Debug($"GetRepodetailsFromPerticularOrder(): No specific repository found. Returning repository or 'Not Found in Repo': {repo}");
            return repo;
        }

        private static string FindRepositoryByKeywords(List<AqlResult> aqlResults, string[] keywords)
        {
            foreach (var keyword in keywords)
            {
                var repo = aqlResults.Find(x => x.Repo.Contains(keyword))?.Repo;
                if (repo != null)
                {
                    return repo;
                }
            }
            return null;
        }
        public static string GetBomFileName(CommonAppSettings appSettings)
        {
            string bomFileName;
            if (appSettings.SW360 != null)
            {
                bomFileName = $"{appSettings.SW360.ProjectName}_Bom.cdx.json";
            }
            else
            {
                bomFileName = FileConstant.basicSBOMName;
            }

            return bomFileName;
        }
        public static string GetDefaultProjectName(CommonAppSettings appSettings)
        {
            string projectName;
            if (appSettings.SW360 != null)
            {
                projectName = appSettings.SW360.ProjectName;
            }
            else
            {
                projectName = FileConstant.basicSBOMName;
            }

            return projectName;
        }
    }
}
