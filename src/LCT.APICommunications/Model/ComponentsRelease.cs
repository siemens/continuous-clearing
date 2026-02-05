// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// ComponentsRelease model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentsRelease
    {
        #region Properties

        /// <summary>
        /// Gets or sets the embedded release data.
        /// </summary>
        [JsonProperty("_embedded")]
        public ReleaseEmbedded Embedded { get; set; }

        #endregion Properties
    }
}