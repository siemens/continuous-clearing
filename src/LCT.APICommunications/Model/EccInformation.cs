// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The EccInformation model
    /// </summary> 
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class EccInformation
    {
        [JsonProperty("eccStatus")]
        public string EccStatus { get; set; }
    }
}