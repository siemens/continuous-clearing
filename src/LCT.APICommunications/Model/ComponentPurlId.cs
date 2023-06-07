// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The Component Level Purl id model
    /// </summary>
    /// 

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentPurlId
    {
        [JsonProperty("externalIds")]
        public Dictionary<string, string> ExternalIds { get; set; }
    }
}