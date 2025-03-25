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
    /// 


    [DataContract]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ExternalIds
    {
        [DataMember(Name = "purl.id")]
        public string Purl_Id { get; set; }

        [DataMember(Name = "package-url")]
        public string Package_Url { get; set; }
    }
}