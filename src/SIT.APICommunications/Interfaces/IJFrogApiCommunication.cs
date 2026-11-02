// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using SIT.APICommunications.Model;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SIT.APICommunications.Interfaces
{
    /// <summary>
    /// Defines the contract for JFrog Artifactory API communication operations.
    /// </summary>
    public interface IJFrogApiCommunication
    {
        /// <summary>
        /// Asynchronously retrieves package information from the Artifactory repository.
        /// </summary>
        /// <param name="component">The component containing the package information request details.</param>
        /// <returns>An HttpResponseMessage containing the package information.</returns>
        Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component);

        /// <summary>
        /// Asynchronously deletes a package from the JFrog repository.
        /// </summary>
        /// <param name="repoName">The name of the repository containing the package.</param>
        /// <param name="componentName">The name of the component to delete.</param>
        /// <returns>An HttpResponseMessage indicating the result of the delete operation.</returns>
        Task<HttpResponseMessage> DeletePackageFromJFrogRepo(string repoName, string componentName);

        /// <summary>
        /// Asynchronously copies a package from a remote repository to the local repository.
        /// </summary>
        /// <param name="component">The component containing the copy operation details.</param>
        /// <returns>An HttpResponseMessage indicating the result of the copy operation.</returns>
        Task<HttpResponseMessage> CopyFromRemoteRepo(ComponentsToArtifactory component);

        /// <summary>
        /// Asynchronously moves a package from one repository to another.
        /// </summary>
        /// <param name="component">The component containing the move operation details.</param>
        /// <returns>An HttpResponseMessage indicating the result of the move operation.</returns>
        Task<HttpResponseMessage> MoveFromRepo(ComponentsToArtifactory component);

        /// <summary>
        /// Asynchronously retrieves the API key from the Artifactory server.
        /// </summary>
        /// <returns>An HttpResponseMessage containing the API key response.</returns>
        Task<HttpResponseMessage> GetApiKey();

        /// <summary>
        /// Asynchronously sets (or updates) item properties on the destination artifact in JFrog
        /// Artifactory after a successful copy/move operation.
        /// </summary>
        /// <param name="component">The component whose destination artifact should be updated.</param>
        /// <param name="properties">The metadata properties to set, keyed by property name.</param>
        /// <returns>An HttpResponseMessage indicating the result of the property update.</returns>
        Task<HttpResponseMessage> SetPackagePropertiesInJfrog(ComponentsToArtifactory component, IReadOnlyDictionary<string, string> properties);
    }
}
