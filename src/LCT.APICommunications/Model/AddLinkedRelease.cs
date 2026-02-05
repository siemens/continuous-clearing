// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// Represents information for adding a linked release.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AddLinkedRelease
    {
        #region Properties

        /// <summary>
        /// Gets or sets the release relation type.
        /// </summary>
        [JsonProperty("releaseRelation")]
        public string ReleaseRelation { get; set; }

        /// <summary>
        /// Gets or sets the comment for the linked release.
        /// </summary>
        [JsonProperty("comment")]
        public string Comment { get; set; }

        #endregion Properties
    }
}
