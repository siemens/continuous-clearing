// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The Sw360Projects class
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Sw360AllProjects
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("clearingState")]
        public string ClearingState { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("_links")]
        public Links Links { get; set; }

        [JsonProperty("linkedReleases")]
        public IList<LinkedReleases> LinkedReleases { get; set; }

    }

}
