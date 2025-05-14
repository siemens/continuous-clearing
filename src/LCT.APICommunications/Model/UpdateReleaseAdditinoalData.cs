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
    public class UpdateReleaseAdditinoalData
    {
        [JsonProperty("additionalData")]
        public Dictionary<string, string> AdditionalData { get; set; }
    }
}
