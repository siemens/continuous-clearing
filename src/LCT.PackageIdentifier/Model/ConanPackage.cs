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
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
        [JsonProperty("context")]
        public string Context { get; set; }
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
        public Dictionary<string, ConanDependency> Dependencies { get; set; }
        
        // Helper properties to make code more readable
        public bool IsRuntimeDependency => Context == "host" && Libs == true && Visible == true && Run == false;
        public bool IsDevDependency => Context == "build" || (Run == true && Build == true);
        public bool IsDirectDependency(string nodeId)
        {
            return Dependencies?.ContainsKey(nodeId) == true && Dependencies[nodeId].Direct == true;
        }
    }

    [ExcludeFromCodeCoverage]
    public class ConanDependency
    {
        [JsonProperty("ref")]
        public string Ref { get; set; }
        [JsonProperty("require")]
        public string Require { get; set; }
        [JsonProperty("run")]
        public bool Run { get; set; }
        [JsonProperty("libs")]
        public bool Libs { get; set; }
        [JsonProperty("skip")]
        public bool Skip { get; set; }
        [JsonProperty("test")]
        public bool Test { get; set; }
        [JsonProperty("force")]
        public bool Force { get; set; }
        [JsonProperty("direct")]
        public bool Direct { get; set; }
        [JsonProperty("build")]
        public bool Build { get; set; }
        [JsonProperty("headers")]
        public bool Headers { get; set; }
        [JsonProperty("visible")]
        public bool Visible { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class ConanDepJson
    {
        [JsonProperty("graph")]
        public ConanGraph Graph { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class ConanGraph
    {
        [JsonProperty("nodes")]
        public Dictionary<string, ConanPackage> Nodes { get; set; }
        [JsonProperty("root")]
        public Dictionary<string, string> Root { get; set; }
    }
}
