// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// The Sw360 AttachmentHash Model
    /// </summary>
    public class Sw360AttachmentHash
    {
        public string AttachmentLink { get; set; }

        public string HashCode { get; set; }

        public string   SourceDownloadUrl { get; set; }

        public string SW360AttachmentName { get; set; }
        public bool isAttachmentSourcenotAvailableInSw360 { get; set; }
    }
}
