// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The Sw360Components Model class
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Sw360Components
    {
        #region Properties

        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the component type.
        /// </summary>
        [JsonProperty("componentType")]
        public string ComponentType { get; set; }

        /// <summary>
        /// Gets or sets the links.
        /// </summary>
        [JsonProperty("_links")]
        public Links Links { get; set; }

        /// <summary>
        /// Gets or sets the external identifiers.
        /// </summary>
        [JsonProperty("externalIds")]
        public ExternalIds ExternalIds { get; set; }

        #endregion Properties
    }
}