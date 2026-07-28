// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace SIT.APICommunications.Model
{
    /// <summary>
    /// Represents a request to update a linked release.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class UpdateLinkedRelease
    {
        #region Properties

        /// <summary>
        /// Gets or sets the comment for the linked release update.
        /// </summary>
        [JsonProperty("comment")]
        public string Comment { get; set; }

        #endregion Properties
    }
}
