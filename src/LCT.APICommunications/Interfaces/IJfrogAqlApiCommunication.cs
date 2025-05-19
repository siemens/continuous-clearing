// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.APICommunications.Interfaces
{
    /// <summary>
    /// The IJfrogAqlApiCommunication interface
    /// </summary>
    public interface IJfrogAqlApiCommunication
    {
        /// <summary>
        /// Gets the internal component data based on repo name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage> GetInternalComponentDataByRepo(string repoName, string correlationId);
        /// <summary>
        /// Gets the internal component data based on repo name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage> GetNpmComponentDataByRepo(string repoName,string correlationId);
        /// <summary>
        /// Gets the internal component data based on repo name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage> GetPypiComponentDataByRepo(string repoName, string correlationId);


        /// <summary>
        /// Gets the package information in the repo, via the name or path
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <param name="packageName">repoName</param>
        /// <param name="path">repoName</param>
        /// <returns>AqlResult</returns>
        Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component = null, string correlationId=null);

        /// <summary>
        /// Checks connectivity with JFrog server
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage> CheckConnection(string correlationId);
    }
}
