// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model
{
    public class UpdateReleaseAdditinoalData
    {
        [JsonProperty("additionalData")]
        public Dictionary<string, string> AdditionalData { get; set; }
    }
}
