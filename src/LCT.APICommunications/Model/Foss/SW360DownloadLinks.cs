// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// The SW360DownloadLinks model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class SW360DownloadLinks
    {
        #region Properties

        /// <summary>
        /// Gets or sets the SW360 download link.
        /// </summary>
        [JsonProperty("sw360:downloadLink")]
        public SW360DownloadHref Sw360DownloadLink { get; set; }

        #endregion Properties
    }
}
