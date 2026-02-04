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
    /// Defines the contract for JFrog AQL (Artifactory Query Language) API communication.
    /// </summary>
    public interface IJfrogAqlApiCommunication
    {
        /// <summary>
        /// Asynchronously gets the internal component data based on repository name.
        /// </summary>
        /// <param name="repoName">The repository name.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> GetInternalComponentDataByRepo(string repoName);

        /// <summary>
        /// Asynchronously gets the NPM component data based on repository name.
        /// </summary>
        /// <param name="repoName">The repository name.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> GetNpmComponentDataByRepo(string repoName);

        /// <summary>
        /// Asynchronously gets the PyPI component data based on repository name.
        /// </summary>
        /// <param name="repoName">The repository name.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> GetPypiComponentDataByRepo(string repoName);

        /// <summary>
        /// Asynchronously gets the Cargo component data based on repository name.
        /// </summary>
        /// <param name="repoName">The repository name.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> GetCargoComponentDataByRepo(string repoName);

        /// <summary>
        /// Asynchronously gets the package information in the repository.
        /// </summary>
        /// <param name="component">The component to query in Artifactory.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component = null);

        /// <summary>
        /// Asynchronously checks connectivity with the JFrog server.
        /// </summary>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> CheckConnection();
    }
}
