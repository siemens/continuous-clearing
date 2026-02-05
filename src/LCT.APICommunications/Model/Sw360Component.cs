// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The SW360Component Model class
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Sw360Component
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