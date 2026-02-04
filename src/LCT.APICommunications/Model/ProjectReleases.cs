// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The ProjectRelease Model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ProjectReleases
    {
        #region Properties

        /// <summary>
        /// Gets or sets the project name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the clearing state.
        /// </summary>
        [JsonProperty("clearingState")]
        public string ClearingState { get; set; }

        /// <summary>
        /// Gets or sets the embedded release information.
        /// </summary>
        [JsonProperty("_embedded")]
        public ReleaseEmbedded Embedded { get; set; }

        /// <summary>
        /// Gets or sets the list of linked releases.
        /// </summary>
        [JsonProperty("linkedReleases")]
        public List<Sw360LinkedRelease> LinkedReleases { get; set; }

        #endregion Properties
    }
}
