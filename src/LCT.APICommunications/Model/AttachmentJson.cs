// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// AttachmentJson model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [Serializable]
    public class AttachmentJson
    {
        #region Properties

        /// <summary>
        /// Gets or sets the filename of the attachment.
        /// </summary>
        [JsonProperty("filename")]
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the attachment content identifier.
        /// </summary>
        [JsonProperty("attachmentContentId")]
        public string AttachmentContentId { get; set; }

        /// <summary>
        /// Gets or sets the attachment type.
        /// </summary>
        [JsonProperty("attachmentType")]
        public string AttachmentType { get; set; }

        /// <summary>
        /// Gets or sets the check status of the attachment.
        /// </summary>
        [JsonProperty("checkStatus")]
        public string CheckStatus { get; set; }

        /// <summary>
        /// Gets or sets the comment provided when the attachment was created.
        /// </summary>
        [JsonProperty("createdComment")]
        public string CreatedComment { get; set; }

        #endregion Properties
    }
}
