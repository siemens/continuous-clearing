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
    /// The UpdateRelease model
    /// </summary>
    public class UpdateRelease
    {
        [JsonProperty("clearingState")]
        public string ClearingState { get; set; } = string.Empty;
    }
}
