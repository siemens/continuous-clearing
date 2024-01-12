// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class FossTriggerStatus
    {
        [JsonProperty("content")]
        public Content Content { get; set; }

        [JsonProperty("_links")]
        public Links Links { get; set; }
    }
}
