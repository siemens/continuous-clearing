// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model.AQL
{
    /// <summary>
    /// The AqlResult model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AqlResult
    {
        [JsonProperty("repo")]
        public string Repo { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("actual_md5")]
        public string MD5 { get; set; }

        [JsonProperty("actual_sha1")]
        public string SHA1 { get; set; }

        [JsonProperty("sha256")]
        public string SHA256 { get; set; }

        public List<AqlProperty> Properties { get; set; }

    }
    public class AqlProperty
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

}
