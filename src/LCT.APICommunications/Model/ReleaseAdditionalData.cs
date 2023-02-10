// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
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
        [DataMember(Name = "fossology url")]
        public string Fossology_url { get; set; }
    }
}
