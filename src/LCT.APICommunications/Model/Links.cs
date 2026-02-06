// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The Link model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Links
    {
        #region Properties

        /// <summary>
        /// Gets or sets the self-referencing link.
        /// </summary>
        [JsonProperty("self")]
        public Self Self { get; set; }

        #endregion Properties
    }
}
