// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
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
        [DataMember(Name = "package download url")]
        public string Download_url { get; set; }
    }
}