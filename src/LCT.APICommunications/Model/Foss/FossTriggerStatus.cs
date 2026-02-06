// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Represents the status response from triggering a Fossology process.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class FossTriggerStatus
    {
        #region Properties

        /// <summary>
        /// Gets or sets the content of the trigger status.
        /// </summary>
        [JsonProperty("content")]
        public Content Content { get; set; }

        /// <summary>
        /// Gets or sets the links associated with the trigger status.
        /// </summary>
        [JsonProperty("_links")]
        public Links Links { get; set; }

        #endregion Properties
    }
}
