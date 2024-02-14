// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// CreateComponent model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class CreateComponent
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("componentType")]
        public string ComponentType { get; set; }

        [JsonProperty("categories")]
        public string[] Categories { get; set; }
        [JsonProperty("externalIds")]
        public ExternalIds ExternalIds { get; set; }
    }
}