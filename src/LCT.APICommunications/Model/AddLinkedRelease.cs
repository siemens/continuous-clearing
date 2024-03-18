// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AddLinkedRelease
    {
        [JsonProperty("releaseRelation")]
        public string ReleaseRelation { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }
    }
}
