// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Runtime.Serialization;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// AdditionalData model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [DataContract]
    public class AdditionalData
    {
        #region Properties

        /// <summary>
        /// Gets or sets the package download URL.
        /// </summary>
        [DataMember(Name = "package download url")]
        public string Download_url { get; set; }

        #endregion Properties
    }
}