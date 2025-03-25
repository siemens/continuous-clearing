// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Checksum
    {
        [JsonProperty("sha1")]
        public string Sha1 { get; set; }
    }
}
