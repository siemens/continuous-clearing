// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Runtime.Serialization;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// ExternalIds model
    /// </summary>
    [DataContract]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ExternalIds
    {
        #region Properties

        /// <summary>
        /// Gets or sets the PURL identifier.
        /// </summary>
        [DataMember(Name = "purl.id")]
        public string Purl_Id { get; set; }

        /// <summary>
        /// Gets or sets the package URL.
        /// </summary>
        [DataMember(Name = "package-url")]
        public string Package_Url { get; set; }

        #endregion Properties
    }
}