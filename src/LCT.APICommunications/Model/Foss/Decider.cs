// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Fossoloygy decider model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Decider
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the Nomos-Monk decider is enabled.
        /// </summary>
        [JsonProperty("nomos_monk")]
        public bool Nomos_monk { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether bulk reused decider is enabled.
        /// </summary>
        [JsonProperty("bulk_reused")]
        public bool Bulk_reused { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the new scanner decider is enabled.
        /// </summary>
        [JsonProperty("new_scanner")]
        public bool New_scanner { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Ojo decider is enabled.
        /// </summary>
        [JsonProperty("ojo_decider")]
        public bool Ojo_decider { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Decider"/> class.
        /// </summary>
        public Decider()
        {
        }

        #endregion Constructors
    }
}
