// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using System.Collections.Generic;
using System.Net.Http;
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
        /// <summary>
        /// Gets the internal component data by Repo name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>IList<AqlResult></returns>
        public Task<IList<AqlResult>> GetNpmComponentDataByRepo(string repoName);
        /// <summary>
        /// Gets the internal component data by Repo name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>IList<AqlResult></returns>
        public Task<IList<AqlResult>> GetPypiComponentDataByRepo(string repoName);

        /// <summary>
        /// Gets the package information in the repo, via the name or path
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <param name="packageName">repoName</param>
        /// <param name="path">repoName</param>
        /// <returns>AqlResult</returns>
#nullable enable
        public Task<AqlResult?> GetPackageInfo(ComponentsToArtifactory component);

        public Task<HttpResponseMessage> CheckJFrogConnectivity(string correlationId);
    }
}
