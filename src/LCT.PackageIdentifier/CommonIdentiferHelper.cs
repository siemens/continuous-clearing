// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
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
        #region Fields
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
        #endregion

        #region Properties
        #endregion

        #region Constructors
        #endregion

        #region Methods
        /// <summary>
        /// Returns the repository name for a prioritized order (release, devdep, dev) from AQL results.
        /// </summary>
        /// <param name="aqlResults">List of AQL results to inspect.</param>
        /// <returns>Repository name matching the preferred order or a sentinel when not found.</returns>
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static string GetRepodetailsFromPerticularOrder(List<AqlResult> aqlResults)
        {
            Logger.Debug("GetRepodetailsFromPerticularOrder(): Starting repository details retrieval from AQL results.");

            if (aqlResults == null)
            {
                Logger.Debug("GetRepodetailsFromPerticularOrder(): No repositories identified from aqlresult. Returning 'Not Found in Repo'.");
                return NotFoundInRepo;
            }

            Logger.DebugFormat("GetRepodetailsFromPerticularOrder(): Total repositories identified from AQL result: {0}", aqlResults.Count);
            var repoKeywords = new[] { "release", "devdep", "dev" };
            string repo = FindRepositoryByKeywords(aqlResults, repoKeywords);

            if (repo != null)
            {
                Logger.DebugFormat("GetRepodetailsFromPerticularOrder(): Found repository: {0}", repo);
                return repo;
            }
            repo = aqlResults.FirstOrDefault()?.Repo ?? NotFoundInRepo;
            Logger.DebugFormat("GetRepodetailsFromPerticularOrder(): No specific repository found. Returning repository or 'Not Found in Repo': {0}", repo);
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

        /// <summary>
        /// Returns the default project name to use in file naming based on SW360 configuration or a fallback.
        /// </summary>
        /// <param name="appSettings">Application settings which may contain SW360 project info.</param>
        /// <returns>Project name string for use as default.</returns>

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

        public static Bom GetCdxGenBomData(List<string> configFiles, CommonAppSettings appSettings, System.Func<string, Bom> parseCycloneDxBom)
        {
            var cdxGenBomData = CommonHelper.GetCdxGenBomData(configFiles, parseCycloneDxBom);
            if (cdxGenBomData?.Components != null)
            {
                cdxGenBomData.Components = [.. cdxGenBomData.Components.Where(c => c.Type != Component.Classification.Application)];
                CycloneDXBomParser.CheckValidComponentsForProjectType(cdxGenBomData.Components, appSettings.ProjectType);
                if (cdxGenBomData.Dependencies != null)
                {
                    CycloneDXBomParser.CheckValidDependenciesForProjectType(cdxGenBomData.Dependencies, appSettings.ProjectType);
                }
                return cdxGenBomData;
            }            
            return null;
        }


        #endregion

    }
}
