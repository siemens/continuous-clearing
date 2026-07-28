// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace SIT.APICommunications.Model
{
    /// <summary>
    /// The Sw360Href model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Sw360Href
    {
        #region Properties

        /// <summary>
        /// Gets or sets the hyperlink reference.
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }

        #endregion Properties
    }
}
