// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.Facade.Interfaces
{
    /// <summary>
    /// The IJFrogApiCommunicationFacade interface
    /// </summary>
    public interface IJfrogAqlApiCommunicationFacade
    {
        /// <summary>
        /// Gets the Internal Component Data By Repo Name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage> GetInternalComponentDataByRepo(string repoName);
        /// <summary>
        /// Gets the Internal Component Data By Repo Name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage> GetNpmComponentDataByRepo(string repoName);
        /// <summary>
        /// Gets the Internal Component Data By Repo Name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage> GetPypiComponentDataByRepo(string repoName);


        /// <summary>
        /// Gets the package information in the repo, via the name or path
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <param name="packageName">repoName</param>
        /// <param name="path">repoName</param>
        /// <returns>AqlResult</returns>
        Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component);
              

        /// <summary>
        /// Checks connectivity with JFrog server
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage> CheckConnection();
    }
}
