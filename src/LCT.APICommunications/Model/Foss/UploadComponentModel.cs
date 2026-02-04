// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// The UploadComponentModel
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class UploadComponentModel
    {
        #region Properties

        /// <summary>
        /// Gets or sets the response code.
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the upload identifier.
        /// </summary>
        [JsonProperty("message")]
        public string UploadId { get; set; }

        /// <summary>
        /// Gets or sets the response type.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        #endregion Properties
    }
}
