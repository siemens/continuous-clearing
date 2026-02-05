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
        #region Properties

        /// <summary>
        /// Gets or sets the SW360 component reference.
        /// </summary>
        public Sw360Href Sw360component { get; set; }

        /// <summary>
        /// Gets or sets the self reference link.
        /// </summary>
        public Self Self { get; set; }

        /// <summary>
        /// Gets or sets the list of CURIES (Compact URI) definitions.
        /// </summary>
        public IList<Cury> Curies { get; set; }

        #endregion Properties
    }
}