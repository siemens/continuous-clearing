// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.Services.Model
{
    /// <summary>
    /// ReleaseCreateStatus model
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ReleaseCreateStatus
    {
        #region Properties
        /// <summary>
        /// Gets or sets a value indicating whether the release was created successfully.
        /// </summary>
        public bool IsCreated { get; set; }

        /// <summary>
        /// Gets or sets the release identifier to link.
        /// </summary>
        public string ReleaseIdToLink { get; set; }

        /// <summary>
        /// Gets or sets the attachment API URL.
        /// </summary>
        public string AttachmentApiUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the release already exists.
        /// </summary>
        public bool ReleaseAlreadyExist { get; set; } = false;
        #endregion
    }
}
