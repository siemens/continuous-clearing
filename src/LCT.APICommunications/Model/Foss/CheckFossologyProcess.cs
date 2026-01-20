// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Represents the Fossology process status check information.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class CheckFossologyProcess
    {
        #region Properties

        /// <summary>
        /// Gets or sets the Fossology process information.
        /// </summary>
        [JsonProperty("fossologyProcessInfo")]
        public FossologyProcessInfo FossologyProcessInfo { get; set; }

        /// <summary>
        /// Gets or sets the status of the Fossology process.
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        #endregion Properties
    }
}
