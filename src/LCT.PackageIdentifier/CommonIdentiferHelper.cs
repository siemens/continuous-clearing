// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model.AQL;
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
    }
}
