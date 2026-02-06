// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    /// <summary>
    /// Represents the configuration settings for the application.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Config
    {
        #region Properties

        /// <summary>
        /// Gets or sets the array of include patterns.
        /// </summary>
        public string[] Include { get; set; }

        /// <summary>
        /// Gets or sets the array of exclude patterns.
        /// </summary>
        public string[] Exclude { get; set; }

        /// <summary>
        /// Gets or sets the Artifactory configuration.
        /// </summary>
        public Artifactory Artifactory { get; set; }

        /// <summary>
        /// Gets or sets the release repository.
        /// </summary>
        public string ReleaseRepo { get; set; }

        /// <summary>
        /// Gets or sets the development dependency repository.
        /// </summary>
        public string DevDepRepo { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents Artifactory repository configuration.
    /// </summary>
    public class Artifactory
    {
        #region Properties

        /// <summary>
        /// Gets or sets the list of third-party repositories.
        /// </summary>
        public List<ThirdPartyRepo> ThirdPartyRepos { get; set; }

        /// <summary>
        /// Gets or sets the array of internal repositories.
        /// </summary>
        public string[] InternalRepos { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the array of development repositories.
        /// </summary>
        public string[] DevRepos { get; set; }

        /// <summary>
        /// Gets or sets the array of remote repositories.
        /// </summary>
        public string[] RemoteRepos { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents a third-party repository configuration.
    /// </summary>
    public class ThirdPartyRepo
    {
        #region Properties

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether upload is enabled.
        /// </summary>
        public bool Upload { get; set; }

        #endregion
    }
}
