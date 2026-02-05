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
    /// Represents a request to update release external identifiers.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class UpdateReleaseExternalId
    {
        #region Properties

        /// <summary>
        /// Gets or sets the external identifiers dictionary.
        /// </summary>
        [JsonProperty("externalIds")]
        public Dictionary<string, string> ExternalIds { get; set; } = new Dictionary<string, string>();

        #endregion Properties
    }
}
