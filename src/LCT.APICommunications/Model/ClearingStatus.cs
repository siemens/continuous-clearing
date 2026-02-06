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
    /// The ReleasesInfo model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ReleasesInfo
    {
        #region Properties

        /// <summary>
        /// Gets or sets the release name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the release version.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the clearing state of the release.
        /// </summary>
        [JsonProperty("clearingState")]
        public string ClearingState { get; set; }

        /// <summary>
        /// Gets or sets the source code download URL.
        /// </summary>
        [JsonProperty("sourceCodeDownloadurl")]
        public string SourceCodeDownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the binary download URL.
        /// </summary>
        [JsonProperty("binaryDownloadurl")]
        public string BinaryDownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the creator of the release.
        /// </summary>
        [JsonProperty("createdBy")]
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the ECC (Export Control Classification) information.
        /// </summary>
        [JsonProperty("eccInformation")]
        public EccInformation EccInformation { get; set; }

        /// <summary>
        /// Gets or sets the links associated with the release.
        /// </summary>
        [JsonProperty("_links")]
        public ReleaseLinks Links { get; set; }

        /// <summary>
        /// Gets or sets additional data as key-value pairs.
        /// </summary>
        [JsonProperty("additionalData")]
        public Dictionary<string, string> AdditionalData { get; set; }

        /// <summary>
        /// Gets or sets the external identifiers as key-value pairs.
        /// </summary>
        [JsonProperty("externalIds")]
        public Dictionary<string, string> ExternalIds { get; set; }

        /// <summary>
        /// Gets or sets the embedded attachment data.
        /// </summary>
        [JsonProperty("_embedded")]
        public AttachmentEmbedded Embedded { get; set; }

        /// <summary>
        /// Gets or sets the list of external tool processes.
        /// </summary>
        [JsonProperty("externalToolProcesses")]
        public List<ExternalToolProcess> ExternalToolProcesses { get; set; }

        #endregion Properties
    }
}
