// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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
        #region Properties

        /// <summary>
        /// Gets or sets the release identifier.
        /// </summary>
        [JsonProperty("release")]
        public string Release { get; set; }

        #endregion Properties
    }
}
