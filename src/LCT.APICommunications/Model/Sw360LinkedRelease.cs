// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// SW360LinkedReleases model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Sw360LinkedRelease
    {
        #region Properties

        /// <summary>
        /// Gets or sets the creator.
        /// </summary>
        [JsonProperty("createdBy")]
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the release identifier.
        /// </summary>
        [JsonProperty("release")]
        public string Release { get; set; }

        /// <summary>
        /// Gets or sets the mainline state.
        /// </summary>
        [JsonProperty("mainlineState")]
        public string MainlineState { get; set; }

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        [JsonProperty("comment")]
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        [JsonProperty("createdOn")]
        public string CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the relation type.
        /// </summary>
        [JsonProperty("relation")]
        public string Relation { get; set; }

        #endregion Properties
    }
}