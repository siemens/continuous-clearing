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
    /// The Sw360Projects mapper model class
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Sw360Projects
    {
        #region Properties

        /// <summary>
        /// Gets or sets the project name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the list of security responsibles.
        /// </summary>
        [JsonProperty("securityResponsibles")]
        public IList<object> SecurityResponsibles { get; set; }

        /// <summary>
        /// Gets or sets the project type.
        /// </summary>
        [JsonProperty("projectType")]
        public string ProjectType { get; set; }

        /// <summary>
        /// Gets or sets the visibility.
        /// </summary>
        [JsonProperty("visibility")]
        public string Visibility { get; set; }

        /// <summary>
        /// Gets or sets the links.
        /// </summary>
        [JsonProperty("_links")]
        public Links Links { get; set; }

        #endregion Properties
    }
}
