// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Fossoloygy decider model
    /// </summary>

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Decider
    {
        [JsonProperty("nomos_monk")]
        public bool Nomos_monk { get; set; }

        [JsonProperty("bulk_reused")]
        public bool Bulk_reused { get; set; }

        [JsonProperty("new_scanner")]
        public bool New_scanner { get; set; }

        [JsonProperty("ojo_decider")]
        public bool Ojo_decider { get; set; }

        public Decider()
        {

        }
    }
}
