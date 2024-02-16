// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model.AQL;
using System.Collections.Generic;

namespace LCT.PackageIdentifier
{
    internal static class CommonIdentiferHelper
    {
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
        public static string GetRepodetailsFromPerticularOrder(List<AqlResult> aqlResults)
        {
            if (aqlResults != null)
            {
                string repoName = aqlResults.Find(x => x.Repo.Contains("release"))?.Repo ?? NotFoundInRepo;
                if (repoName == NotFoundInRepo)
                {
                    repoName = aqlResults.Find(x => x.Repo.Contains("devdep"))?.Repo ?? NotFoundInRepo;
                    if (repoName == NotFoundInRepo)
                    {
                        return aqlResults[0].Repo ?? NotFoundInRepo;
                    }
                }
                return repoName;
            }
            return NotFoundInRepo;
        }
    }
}
