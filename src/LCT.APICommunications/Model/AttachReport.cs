// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
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
        public string ReleaseId { get; set; }

        public string AttachmentType { get; set; }

        public string AttachmentFile { get; set; }

        public string AttachmentCheckStatus { get; set; }

        public string AttachmentReleaseComment { get; set; } = string.Empty;
    }
}
