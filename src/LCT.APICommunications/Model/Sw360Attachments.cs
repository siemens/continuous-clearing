// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// Sw360Attachments model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Sw360Attachments
    {
        #region Properties

        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        [JsonProperty("filename")]
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the SHA1 hash.
        /// </summary>
        [JsonProperty("sha1")]
        public string Sha1 { get; set; }

        /// <summary>
        /// Gets or sets the attachment type.
        /// </summary>
        [JsonProperty("attachmentType")]
        public string AttachmentType { get; set; }

        /// <summary>
        /// Gets or sets the attachment links.
        /// </summary>
        [JsonProperty("_links")]
        public AttachmentLinks Links { get; set; }

        #endregion Properties
    }
}