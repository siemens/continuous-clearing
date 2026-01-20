// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The ProjectsMapper model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ProjectsMapper
    {
        #region Properties

        /// <summary>
        /// Gets or sets the embedded project information.
        /// </summary>
        [JsonProperty("_embedded")]
        public ProjectEmbedded Embedded { get; set; }

        #endregion Properties
    }
}