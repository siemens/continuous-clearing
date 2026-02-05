// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// The Sw360 AttachmentHash Model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Sw360AttachmentHash
    {
        #region Properties

        /// <summary>
        /// Gets or sets the attachment link.
        /// </summary>
        public string AttachmentLink { get; set; }

        /// <summary>
        /// Gets or sets the hash code of the attachment.
        /// </summary>
        public string HashCode { get; set; }

        /// <summary>
        /// Gets or sets the source download URL.
        /// </summary>
        public string SourceDownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the SW360 attachment name.
        /// </summary>
        public string SW360AttachmentName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the attachment source is not available in SW360.
        /// </summary>
        public bool isAttachmentSourcenotAvailableInSw360 { get; set; }

        #endregion Properties
    }
}
