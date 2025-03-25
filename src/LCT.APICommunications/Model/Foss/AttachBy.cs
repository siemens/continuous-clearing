// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AttachBy
    {
        [JsonProperty("createdBy")]
        public AttachCreatedBy CreatedBy { get; set; }
    }
}
