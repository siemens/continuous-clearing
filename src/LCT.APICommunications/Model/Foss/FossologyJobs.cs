// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Fossology Jobs model
    /// </summary>

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class FossologyJobs
    {
        [JsonProperty("analysis")]
        public Analysis Analysis { get; set; }

        [JsonProperty("decider")]
        public Decider Decider { get; set; }

        [JsonProperty("reuse")]
        public Reuse Reuse { get; set; }
    }
}
