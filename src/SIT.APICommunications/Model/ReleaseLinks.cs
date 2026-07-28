// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace SIT.APICommunications.Model
{
    /// <summary>
    /// The ReleaseLinks Model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ReleaseLinks
    {
        #region Properties

        /// <summary>
        /// Gets or sets the self-referencing link.
        /// </summary>
        [JsonProperty("self")]
        public Self Self { get; set; }

        /// <summary>
        /// Gets or sets the SW360 component information.
        /// </summary>
        [JsonProperty("sw360:component")]
        public Sw360Component Sw360Component { get; set; }

        #endregion Properties
    }
}
