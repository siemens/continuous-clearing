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
    /// The Component Level Purl id model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentPurlId
    {
        #region Properties

        /// <summary>
        /// Gets or sets the external identifiers as key-value pairs.
        /// </summary>
        [JsonProperty("externalIds")]
        public Dictionary<string, string> ExternalIds { get; set; }

        #endregion Properties
    }
}