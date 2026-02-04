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
    /// ComponentEmbedded model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentEmbedded
    {
        #region Properties

        /// <summary>
        /// Gets or sets the list of SW360 components.
        /// </summary>
        [JsonProperty("sw360:components")]
        public IList<Sw360Components> Sw360components { get; set; }

        #endregion Properties
    }
}
