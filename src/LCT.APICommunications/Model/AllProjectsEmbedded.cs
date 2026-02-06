// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
// SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The ProjectsEmbedded class
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AllProjectsEmbedded
    {
        #region Properties

        /// <summary>
        /// Gets or sets the list of all SW360 projects.
        /// </summary>
        [JsonProperty("sw360:projects")]
        public IList<Sw360AllProjects> Sw360Allprojects { get; set; }

        #endregion Properties
    }
}
