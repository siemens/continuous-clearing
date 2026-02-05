// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Represents content with a message.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Content
    {
        #region Properties

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        #endregion Properties
    }
}
