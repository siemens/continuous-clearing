// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// Releases model
    /// </summary>
    public class Releases
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("componentId")]
        public string ComponentId { get; set; }

        [JsonProperty("sourceCodeDownloadurl")]
        public string SourceDownloadurl { get; set; }

        [JsonProperty("binaryDownloadurl")]
        public string BinaryDownloadUrl { get; set; }

        [JsonProperty("createdOn")]
        public string CreatedOn { get; set; }

        [JsonProperty("clearingState")]
        public string ClearingState { get; set; }

        [JsonProperty("_links")]
        public Links Links { get; set; }

        [JsonProperty("externalIds")]
        public ExternalIds ExternalIds { get; set; }

    }
}
