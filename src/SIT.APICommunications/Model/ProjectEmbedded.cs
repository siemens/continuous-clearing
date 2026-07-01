// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace SIT.APICommunications.Model
{
    /// <summary>
    /// ProjectEmbeded json mapping model class
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ProjectEmbedded
    {
        #region Properties

        /// <summary>
        /// Gets or sets the list of SW360 projects.
        /// </summary>
        [JsonProperty("sw360:projects")]
        public IList<Sw360Projects> Sw360projects { get; set; }

        #endregion Properties
    }
}
