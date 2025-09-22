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
    /// <summary>
    /// Represents a package in a Conan 2.0 dep.json file
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConanPackage
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        [JsonProperty("context")]
        public string Context { get; set; } = string.Empty;

        [JsonProperty("libs")]
        public bool? Libs { get; set; }

        [JsonProperty("visible")]
        public bool? Visible { get; set; }

        [JsonProperty("run")]
        public bool? Run { get; set; }

        [JsonProperty("build")]
        public bool? Build { get; set; }

        [JsonProperty("test")]
        public bool? Test { get; set; }

        [JsonProperty("dependencies")]
        public Dictionary<string, ConanDependency> Dependencies { get; set; } = new Dictionary<string, ConanDependency>();
        
        // Helper properties to make code more readable
        public bool IsRuntimeDependency => Context == "host" && Libs == true && Visible == true && Run == false;
        public bool IsDevDependency => Context == "build" || (Run == true && Build == true);
        
        public bool IsDirectDependency(string nodeId)
        {
            return Dependencies?.ContainsKey(nodeId) == true && Dependencies[nodeId].Direct == true;
        }
    }

    /// <summary>
    /// Represents the root structure of a Conan 2.0 dep.json file
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConanDepJson
    {
        [JsonProperty("graph")]
        public ConanGraph Graph { get; set; }
    }

    /// <summary>
    /// Represents the graph section in a Conan 2.0 dep.json file
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConanGraph
    {
        [JsonProperty("nodes")]
        public Dictionary<string, ConanPackage> Nodes { get; set; } = new Dictionary<string, ConanPackage>();

        [JsonProperty("root")]
        public Dictionary<string, string> Root { get; set; } = new Dictionary<string, string>();
    }
}
