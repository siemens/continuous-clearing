// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// ComponentsModel 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentsModel
    {
        #region Properties

        /// <summary>
        /// Gets or sets the embedded component data.
        /// </summary>
        [JsonProperty("_embedded")]
        public ComponentEmbedded Embedded { get; set; }

        #endregion Properties
    }
}