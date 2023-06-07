// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The Sw360Components Model class
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Sw360Components
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("componentType")]
        public string ComponentType { get; set; }

        [JsonProperty("_links")]
        public Links Links { get; set; }

        [JsonProperty("externalIds")]
        public ExternalIds ExternalIds { get; set; }
    }
}