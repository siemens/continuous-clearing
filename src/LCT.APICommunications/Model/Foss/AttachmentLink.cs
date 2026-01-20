// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// AttachmentLink class
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AttachmentLink
    {
        #region Properties

        /// <summary>
        /// Gets or sets the filename of the attachment.
        /// </summary>
        [JsonProperty("filename")]
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the SHA1 hash of the attachment.
        /// </summary>
        [JsonProperty("sha1")]
        public string Sha1 { get; set; }

        /// <summary>
        /// Gets or sets the attachment type.
        /// </summary>
        [JsonProperty("attachmentType")]
        public string AttachmentType { get; set; }

        /// <summary>
        /// Gets or sets the creator of the attachment.
        /// </summary>
        [JsonProperty("createdBy")]
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the team that created the attachment.
        /// </summary>
        [JsonProperty("createdTeam")]
        public string CreatedTeam { get; set; }

        /// <summary>
        /// Gets or sets the comment provided when the attachment was created.
        /// </summary>
        [JsonProperty("createdComment")]
        public string CreatedComment { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the attachment.
        /// </summary>
        [JsonProperty("createdOn")]
        public string CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the comment provided when the attachment was checked.
        /// </summary>
        [JsonProperty("checkedComment")]
        public string CheckedComment { get; set; }

        /// <summary>
        /// Gets or sets the check status of the attachment.
        /// </summary>
        [JsonProperty("checkStatus")]
        public string CheckStatus { get; set; }

        /// <summary>
        /// Gets or sets the SW360 download links for the attachment.
        /// </summary>
        [JsonProperty("_links")]
        public SW360DownloadLinks Links { get; set; }

        /// <summary>
        /// Gets or sets the self reference link.
        /// </summary>
        [JsonProperty("self")]
        public Self Self { get; set; }

        /// <summary>
        /// Gets or sets the list of CURIES (Compact URI) definitions.
        /// </summary>
        [JsonProperty("curies")]
        public IList<Cury> Curies { get; set; }

        #endregion Properties
    }
}
