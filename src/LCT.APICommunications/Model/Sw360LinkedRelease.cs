// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// SW360LinkedReleases model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Sw360LinkedRelease
    {
        [JsonProperty("createdBy")]
        public string CreatedBy { get; set; }

        [JsonProperty("release")]
        public string Release { get; set; }

        [JsonProperty("mainlineState")]
        public string MainlineState { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }
        
        [JsonProperty("createdOn")]
        public string CreatedOn { get; set; }
        
        [JsonProperty("relation")]
        public string Relation { get; set; }
    }
}