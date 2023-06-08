// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// THe LinkedRelases Class
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class LinkedReleases
    {
        [JsonProperty("release")]
        public string Release { get; set; }
    }
}
