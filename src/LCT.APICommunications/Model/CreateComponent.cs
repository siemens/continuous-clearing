// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// CreateComponent model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class CreateComponent
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
        /// Gets or sets the component categories.
        /// </summary>
        [JsonProperty("categories")]
        public string[] Categories { get; set; }

        /// <summary>
        /// Gets or sets the external identifiers.
        /// </summary>
        [JsonProperty("externalIds")]
        public ExternalIds ExternalIds { get; set; }

        #endregion Properties
    }
}