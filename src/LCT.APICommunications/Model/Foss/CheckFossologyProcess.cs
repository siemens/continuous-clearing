﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class CheckFossologyProcess
    {
        [JsonProperty("fossologyProcessInfo")]
        public FossologyProcessInfo FossologyProcessInfo { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }

    }
}
