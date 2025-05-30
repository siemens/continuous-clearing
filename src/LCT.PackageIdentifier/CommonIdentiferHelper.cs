﻿// --------------------------------------------------------------------------------------------------------------------
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
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
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
