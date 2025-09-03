// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT

// --------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    [ExcludeFromCodeCoverage]
    public class ConanPackage
    {
        public string Id { get; set; }
        [JsonProperty("ref")]
        public string Reference { get; set; } = string.Empty;
        [JsonProperty("requires")]
        public List<string> Dependencies { get; set; }
        [JsonProperty("build_requires")]
        public List<string> DevDependencies { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Conan2LockFile
    {
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("requires")]
        public List<string> Requires { get; set; }
        [JsonProperty("build_requires")]
        public List<string> BuildRequires { get; set; }
        [JsonProperty("python_requires")]
        public List<string> PythonRequires { get; set; }
        [JsonProperty("config_requires")]
        public List<string> ConfigRequires { get; set; }
        [JsonProperty("tool_requires")]
        public List<string> ToolRequires { get; set; }
    }
}
