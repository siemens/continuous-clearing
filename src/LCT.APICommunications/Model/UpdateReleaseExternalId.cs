// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class UpdateReleaseExternalId
    {
        [JsonProperty("externalIds")]
        public Dictionary<string, string> ExternalIds { get; set; } = new Dictionary<string, string>();
    }
}
