// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The ReleaseLinks Model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ReleaseLinks
    {
        [JsonProperty("self")]
        public Self Self { get; set; }

        [JsonProperty("sw360:component")]
        public Sw360Component Sw360Component { get; set; }
    }
}
