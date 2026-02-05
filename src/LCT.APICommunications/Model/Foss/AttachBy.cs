// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Represents attachment creator information.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AttachBy
    {
        #region Properties

        /// <summary>
        /// Gets or sets the creator information for the attachment.
        /// </summary>
        [JsonProperty("createdBy")]
        public AttachCreatedBy CreatedBy { get; set; }

        #endregion Properties
    }
}
