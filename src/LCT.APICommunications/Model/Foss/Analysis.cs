// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Fossology Analyzer model
    /// </summary>
    /// 

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Analysis
    {
        [JsonProperty("bucket")]
        public bool Bucket { get; set; }

        [JsonProperty("copyright_email_author")]
        public bool Copyright_email_author { get; set; }

        [JsonProperty("ecc")]
        public bool Ecc { get; set; }

        [JsonProperty("keyword")]
        public bool Keyword { get; set; }

        [JsonProperty("mime")]
        public bool Mime { get; set; }

        [JsonProperty("monk")]
        public bool Monk { get; set; }

        [JsonProperty("nomos")]
        public bool Nomos { get; set; }

        [JsonProperty("ojo")]
        public bool Ojo { get; set; }

        [JsonProperty("package")]
        public bool Package { get; set; }

        public Analysis()
        {

        }
    }
}
