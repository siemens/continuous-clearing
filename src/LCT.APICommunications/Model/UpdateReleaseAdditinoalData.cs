// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// Represents a request to update release additional data.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class UpdateReleaseAdditinoalData
    {
        #region Properties

        /// <summary>
        /// Gets or sets the additional data dictionary.
        /// </summary>
        [JsonProperty("additionalData")]
        public Dictionary<string, string> AdditionalData { get; set; }

        #endregion Properties
    }
}
