// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// Releases model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Releases
    {
        #region Properties

        /// <summary>
        /// Gets or sets the release name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the component identifier.
        /// </summary>
        [JsonProperty("componentId")]
        public string ComponentId { get; set; }

        /// <summary>
        /// Gets or sets the source code download URL.
        /// </summary>
        [JsonProperty("sourceCodeDownloadurl")]
        public string SourceDownloadurl { get; set; }

        /// <summary>
        /// Gets or sets the binary download URL.
        /// </summary>
        [JsonProperty("binaryDownloadurl")]
        public string BinaryDownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        [JsonProperty("createdOn")]
        public string CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the clearing state.
        /// </summary>
        [JsonProperty("clearingState")]
        public string ClearingState { get; set; }

        /// <summary>
        /// Gets or sets the links.
        /// </summary>
        [JsonProperty("_links")]
        public Links Links { get; set; }

        /// <summary>
        /// Gets or sets the external identifiers.
        /// </summary>
        [JsonProperty("externalIds")]
        public ExternalIds ExternalIds { get; set; }

        #endregion Properties
    }
}
