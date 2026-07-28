// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;

namespace SIT.APICommunications.Model
{
    /// <summary>
    /// Represents component type data with embedded component information.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentTypeData
    {
        #region Properties

        /// <summary>
        /// Gets or sets the embedded component information.
        /// </summary>
        [JsonProperty("_embedded")]
        public ComponentEmbedded Embedded { get; set; }

        #endregion Properties
    }
}
