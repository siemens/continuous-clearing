// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCD.Common.Models
{
    /// <summary>
    /// Represents upload information.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class UploadInfo
    {
        #region Properties

        /// <summary>
        /// Gets or sets the upload identifier.
        /// </summary>
        public string uploadId { get; set; }

        /// <summary>
        /// Gets or sets the folder identifier.
        /// </summary>
        public string folderId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public string userId { get; set; }

        #endregion Properties
    }
}
