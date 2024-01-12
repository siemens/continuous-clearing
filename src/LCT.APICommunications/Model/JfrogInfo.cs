// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
namespace LCT.APICommunications.Model
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class JfrogInfo
    {
        [JsonProperty("repo")]
        public string Repo { get; set; }
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("checksums")]
        public Checksum Checksum { get; set; }
    }
}
