// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
using System.Collections.Generic;
using System.Linq;

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
        public static string GetRepodetailsFromPerticularOrder(List<AqlResult> aqlResults)
        {
            if (aqlResults == null)
            {
                return NotFoundInRepo;
            }

            if (aqlResults.Find(x => x.Repo.Contains("release"))?.Repo != null)
            {
                return aqlResults.Find(x => x.Repo.Contains("release"))?.Repo;
            }
            else if (aqlResults.Find(x => x.Repo.Contains("devdep"))?.Repo != null)
            {
                return aqlResults.Find(x => x.Repo.Contains("devdep"))?.Repo;
            }
            else if (aqlResults.Find(x => x.Repo.Contains("dev"))?.Repo != null)
            {
                return aqlResults.Find(x => x.Repo.Contains("dev"))?.Repo;
            }
            else
            {
                return aqlResults.FirstOrDefault()?.Repo ?? NotFoundInRepo;
            }
        }

        /// <summary>
        /// Builds the BOM file name based on SW360 project settings or falls back to the basic SBOM name.
        /// </summary>
        /// <param name="appSettings">Application settings containing SW360 configuration.</param>
        /// <returns>Filename to use for the BOM output.</returns>
        public static string GetBomFileName(CommonAppSettings appSettings)
        {
            string bomFileName;
            if (appSettings.SW360 != null)
            {
                bomFileName = $"{appSettings.SW360.ProjectName}_Bom.cdx.json";
            }
            else
            {
                bomFileName = FileConstant.basicSBOMName + "_Bom.cdx.json";
            }

            return bomFileName;
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
        #endregion

        #region Events
        #endregion
    }
}
