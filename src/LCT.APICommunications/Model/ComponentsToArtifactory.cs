// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Net.Http;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// Represents component information for Artifactory upload operations.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentsToArtifactory
    {
        #region Properties

        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the package name.
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// Gets or sets the component version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the component type.
        /// </summary>
        public string ComponentType { get; set; }

        /// <summary>
        /// Gets or sets the JFrog API URL.
        /// </summary>
        public string JfrogApi { get; set; }

        /// <summary>
        /// Gets or sets the source repository name.
        /// </summary>
        public string SrcRepoName { get; set; }

        /// <summary>
        /// Gets or sets the source repository path with full name.
        /// </summary>
        public string SrcRepoPathWithFullName { get; set; }

        /// <summary>
        /// Gets or sets the PyPI or NPM component name.
        /// </summary>
        public string PypiOrNpmCompName { get; set; }

        /// <summary>
        /// Gets or sets the destination repository name.
        /// </summary>
        public string DestRepoName { get; set; }

        /// <summary>
        /// Gets or sets the authentication token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the package information API URL.
        /// </summary>
        public string PackageInfoApiUrl { get; set; }

        /// <summary>
        /// Gets or sets the copy package API URL.
        /// </summary>
        public string CopyPackageApiUrl { get; set; }

        /// <summary>
        /// Gets or sets the move package API URL.
        /// </summary>
        public string MovePackageApiUrl { get; set; }

        /// <summary>
        /// Gets or sets the package file extension.
        /// </summary>
        public string PackageExtension { get; set; }

        /// <summary>
        /// Gets or sets the package path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the package type.
        /// </summary>
        public PackageType PackageType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a dry run operation.
        /// </summary>
        public bool DryRun { get; set; } = true;

        /// <summary>
        /// Gets or sets the package URL (PURL).
        /// </summary>
        public string Purl { get; set; }

        /// <summary>
        /// Gets or sets the JFrog package name.
        /// </summary>
        public string JfrogPackageName { get; set; }

        /// <summary>
        /// Gets or sets the operation type.
        /// </summary>
        public string OperationType { get; set; }

        /// <summary>
        /// Gets or sets the dry run suffix.
        /// </summary>
        public string DryRunSuffix { get; set; }

        /// <summary>
        /// Gets or sets the JFrog repository path.
        /// </summary>
        public string JfrogRepoPath { get; set; }

        /// <summary>
        /// Gets or sets the HTTP response message.
        /// </summary>
        public HttpResponseMessage ResponseMessage { get; set; }

        #endregion Properties
    }
}
