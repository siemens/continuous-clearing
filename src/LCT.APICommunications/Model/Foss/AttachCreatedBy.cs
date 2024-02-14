// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// AttachCreatedBy model class
    /// </summary>

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AttachCreatedBy
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("_links")]
        public Links Links { get; set; }
    }
}
