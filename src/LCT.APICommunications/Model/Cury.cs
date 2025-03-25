// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// Cury model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Cury
    {
        [JsonProperty("href")]
        public string Href { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("templated")]
        public bool Templated { get; set; }
    }
}