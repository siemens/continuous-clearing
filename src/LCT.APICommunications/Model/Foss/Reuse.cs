// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Reuse scanner model
    /// </summary>
    public class Reuse
    {
        [JsonProperty("reuse_upload")]
        public int Reuse_upload { get; set; }

        [JsonProperty("reuse_group")]
        public int Reuse_group { get; set; }

        [JsonProperty("reuse_main")]
        public bool Reuse_main { get; set; }

        [JsonProperty("reuse_enhanced")]
        public bool Reuse_enhanced { get; set; }
    }
}
