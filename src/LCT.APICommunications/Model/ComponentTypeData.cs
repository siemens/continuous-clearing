// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// --------------------------------------------------------------------------------------------------------------------
using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentTypeData
    {
        [JsonProperty("_embedded")]
        public ComponentEmbedded Embedded { get; set; }
    }
}
