// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.AQL
{
    /// <summary>
    /// The AqlResult model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AqlResult
    {
        [JsonProperty("repo")]
        public string Repo { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
