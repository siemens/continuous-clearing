// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Collections.Generic;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// AttachmentLinks model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AttachmentLinks
    {
        public Sw360Href Sw360component { get; set; }
        public Self Self { get; set; }
        public IList<Cury> Curies { get; set; }
    }
}