// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model
{
    public class UpdateReleaseExternalId
    {
        [JsonProperty("externalIds")]
        public Dictionary<string, string> ExternalIds { get; set; } = new Dictionary<string, string>();
    }
}
