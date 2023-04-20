// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;


namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The ProjectsMapper model
    /// </summary>


    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ProjectsMapper
    {
        [JsonProperty("_embedded")]
        public ProjectEmbedded Embedded { get; set; }
    }
}