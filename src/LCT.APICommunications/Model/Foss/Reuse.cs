// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Reuse scanner model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Reuse
    {
        #region Properties

        /// <summary>
        /// Gets or sets the reuse upload identifier.
        /// </summary>
        [JsonProperty("reuse_upload")]
        public int Reuse_upload { get; set; }

        /// <summary>
        /// Gets or sets the reuse group identifier.
        /// </summary>
        [JsonProperty("reuse_group")]
        public int Reuse_group { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether main reuse is enabled.
        /// </summary>
        [JsonProperty("reuse_main")]
        public bool Reuse_main { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether enhanced reuse is enabled.
        /// </summary>
        [JsonProperty("reuse_enhanced")]
        public bool Reuse_enhanced { get; set; }

        #endregion Properties
    }
}
