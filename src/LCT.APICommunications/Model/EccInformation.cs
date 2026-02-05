// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The EccInformation model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class EccInformation
    {
        #region Properties

        /// <summary>
        /// Gets or sets the ECC status.
        /// </summary>
        [JsonProperty("eccStatus")]
        public string EccStatus { get; set; }

        #endregion Properties
    }
}