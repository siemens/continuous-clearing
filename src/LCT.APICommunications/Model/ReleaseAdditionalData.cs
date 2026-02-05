// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Runtime.Serialization;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The ReleaseAdditionalData model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [DataContract]
    public class ReleaseAdditionalData
    {
        #region Properties

        /// <summary>
        /// Gets or sets the FOSSology URL.
        /// </summary>
        [DataMember(Name = "fossology url")]
        public string Fossology_url { get; set; }

        #endregion Properties
    }
}
