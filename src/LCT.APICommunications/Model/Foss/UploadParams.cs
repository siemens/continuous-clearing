// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// The fossology upload package params model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class UploadParams
    {
        #region Properties

        /// <summary>
        /// Gets or sets the folder identifier.
        /// </summary>
        public string FolderId { get; set; }

        /// <summary>
        /// Gets or sets the upload description.
        /// </summary>
        public string UploadDescription { get; set; }

        /// <summary>
        /// Gets or sets the public visibility setting.
        /// </summary>
        public string Public { get; set; }

        #endregion Properties
    }
}
