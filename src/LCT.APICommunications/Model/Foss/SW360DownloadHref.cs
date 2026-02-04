// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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
        #region Properties

        /// <summary>
        /// Gets or sets the download URL.
        /// </summary>
        [JsonProperty("href")]
        public string DownloadUrl { get; set; }

        #endregion Properties
    }
}
