// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// ReleaseEmbedded model
    /// </summary>
    /// 

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ReleaseEmbedded
    {
        [JsonProperty("sw360:releases")]
        public IList<Sw360Releases> Sw360Releases { get; set; }
    }
}