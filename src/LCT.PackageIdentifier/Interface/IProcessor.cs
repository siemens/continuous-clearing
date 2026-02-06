// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier.Interface
{
    /// <summary>
    /// Processor interface
    /// </summary>
    public interface IProcessor
    {
        /// <summary>
        /// Asynchronously checks if components are internal components in JFrog Artifactory.
        /// </summary>
        /// <param name="appSettings">The common application settings.</param>
        /// <param name="artifactoryUpload">The Artifactory credentials.</param>
        /// <param name="component">The component to check.</param>
        /// <param name="repo">The repository name.</param>
        /// <returns>A task representing the asynchronous operation that returns a list of components.</returns>
        public Task<List<Component>> CheckInternalComponentsInJfrogArtifactory(CommonAppSettings appSettings, ArtifactoryCredentials artifactoryUpload, Component component, string repo);

        /// <summary>
        /// Asynchronously gets JFrog Artifactory repository information for a component.
        /// </summary>
        /// <param name="appSettings">The common application settings.</param>
        /// <param name="artifactoryUpload">The Artifactory credentials.</param>
        /// <param name="component">The component to get information for.</param>
        /// <param name="repo">The repository name.</param>
        /// <returns>A task representing the asynchronous operation that returns a list of components.</returns>
        public Task<List<Component>> GetJfrogArtifactoryRepoInfo(CommonAppSettings appSettings, ArtifactoryCredentials artifactoryUpload, Component component, string repo);
    }
}
