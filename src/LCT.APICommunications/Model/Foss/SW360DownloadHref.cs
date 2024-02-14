// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// The SW360DownloadHref model
    /// </summary>

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class SW360DownloadHref
    {

        [JsonProperty("href")]
        public string DownloadUrl { get; set; }
    }
}
