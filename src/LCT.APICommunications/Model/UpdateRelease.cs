// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The UpdateRelease model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class UpdateRelease
    {
        #region Properties

        /// <summary>
        /// Gets or sets the clearing state.
        /// </summary>
        [JsonProperty("clearingState")]
        public string ClearingState { get; set; } = string.Empty;

        #endregion Properties
    }
}
