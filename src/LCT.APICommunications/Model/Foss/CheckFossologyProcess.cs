// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
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
        public FossologyProcessInfo fossologyProcessInfo { get; set; }

    }
}
