// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model.AQL;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LCT.Services.Interface
{
    /// <summary>
    /// The IJFrogService interface
    /// </summary>
    public interface IJFrogService
    {
        /// <summary>
        /// Gets the internal component data by Repo name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>IList<AqlResult></returns>
        public Task<IList<AqlResult>> GetInternalComponentDataByRepo(string repoName);
    }
}
