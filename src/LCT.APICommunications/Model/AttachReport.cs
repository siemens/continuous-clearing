// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// Attach report to sw360 model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AttachReport
    {
        #region Properties

        /// <summary>
        /// Gets or sets the release identifier.
        /// </summary>
        public string ReleaseId { get; set; }

        /// <summary>
        /// Gets or sets the attachment type.
        /// </summary>
        public string AttachmentType { get; set; }

        /// <summary>
        /// Gets or sets the attachment file path.
        /// </summary>
        public string AttachmentFile { get; set; }

        /// <summary>
        /// Gets or sets the attachment check status.
        /// </summary>
        public string AttachmentCheckStatus { get; set; }

        /// <summary>
        /// Gets or sets the attachment release comment.
        /// </summary>
        public string AttachmentReleaseComment { get; set; } = string.Empty;

        #endregion Properties
    }
}
