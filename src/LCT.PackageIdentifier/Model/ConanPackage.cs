
// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
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
}
