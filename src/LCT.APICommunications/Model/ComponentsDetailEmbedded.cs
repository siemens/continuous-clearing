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
    /// Represents embedded component details data.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentsDetailEmbedded
    {
        #region Properties

        /// <summary>
        /// Gets or sets the creator information.
        /// </summary>
        [JsonProperty("createdBy")]
        public CreatedBy CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the list of SW360 releases.
        /// </summary>
        [JsonProperty("sw360:releases")]
        public IList<Sw360Releases> Sw360Releases { get; set; }

        #endregion Properties
    }
}
