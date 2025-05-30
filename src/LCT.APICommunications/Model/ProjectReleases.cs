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
    /// 

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ProjectReleases
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("state")]
        public string state { get; set; }

        [JsonProperty("clearingState")]
        public string clearingState { get; set; }

        [JsonProperty("_embedded")]
        public ReleaseEmbedded Embedded { get; set; }

        [JsonProperty("linkedReleases")]
        public List<Sw360LinkedRelease> LinkedReleases { get; set; }
    }
}
