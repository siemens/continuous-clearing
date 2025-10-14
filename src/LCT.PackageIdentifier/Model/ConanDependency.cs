// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT

// --------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    /// <summary>
    /// Represents a dependency in a Conan 2.0 dep.json file
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConanDependency
    {
        [JsonProperty("ref")]
        public string Ref { get; set; } = string.Empty;

        [JsonProperty("require")]
        public string Require { get; set; } = string.Empty;

        [JsonProperty("run")]
        public bool? Run { get; set; }

        [JsonProperty("libs")]
        public bool? Libs { get; set; }

        [JsonProperty("skip")]
        public bool? Skip { get; set; }

        [JsonProperty("test")]
        public bool? Test { get; set; }

        [JsonProperty("force")]
        public bool? Force { get; set; }

        [JsonProperty("direct")]
        public bool? Direct { get; set; }

        [JsonProperty("build")]
        public bool? Build { get; set; }

        [JsonProperty("transitive_headers")]
        public object TransitiveHeaders { get; set; }

        [JsonProperty("transitive_libs")]
        public object TransitiveLibs { get; set; }

        [JsonProperty("headers")]
        public bool? Headers { get; set; }

        [JsonProperty("package_id_mode")]
        public string PackageIdMode { get; set; } = string.Empty;

        [JsonProperty("visible")]
        public bool? Visible { get; set; }
    }
}