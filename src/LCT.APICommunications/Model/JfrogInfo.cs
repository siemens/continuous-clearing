// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// Represents JFrog Artifactory repository information.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class JfrogInfo
    {
        #region Properties

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        [JsonProperty("repo")]
        public string Repo { get; set; }

        /// <summary>
        /// Gets or sets the path within the repository.
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the checksum information.
        /// </summary>
        [JsonProperty("checksums")]
        public Checksum Checksum { get; set; }

        #endregion Properties
    }
}
