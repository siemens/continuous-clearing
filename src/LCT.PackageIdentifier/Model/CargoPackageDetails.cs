// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.PackageIdentifier.Model
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class CargoPackageDetails
    {
        [JsonProperty("packages")]
        public List<Package> Packages { get; set; }

        [JsonProperty("resolve")]
        public Resolve ResolveInfo { get; set; }
        [JsonProperty("workspace_members")]
        public List<string> Workspace_members { get; set; }
        [JsonProperty("workspace_default_members")]
        public List<string> Workspace_default_members { get; set; }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public class Dep
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("pkg")]
            public string Pkg { get; set; }

            [JsonProperty("dep_kinds")]
            public List<DepKind> DepKinds { get; set; }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public class DepKind
        {
            [JsonProperty("kind")]
            public string Kind { get; set; }

            [JsonProperty("target")]
            public string Target { get; set; }
        }
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public class Node
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("dependencies")]
            public List<string> Dependencies { get; set; }

            [JsonProperty("deps")]
            public List<Dep> Deps { get; set; }

            [JsonProperty("features")]
            public List<string> Features { get; set; }
        }
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public class Package
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("version")]
            public string Version { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("license")]
            public string License { get; set; }

            [JsonProperty("license_file")]
            public object LicenseFile { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("source")]
            public string Source { get; set; }

            [JsonProperty("manifest_path")]
            public string ManifestPath { get; set; }

            [JsonProperty("publish")]
            public object Publish { get; set; }

            [JsonProperty("authors")]
            public List<string> Authors { get; set; }

            [JsonProperty("categories")]
            public List<string> Categories { get; set; }

            [JsonProperty("keywords")]
            public List<string> Keywords { get; set; }

            [JsonProperty("readme")]
            public string Readme { get; set; }

            [JsonProperty("repository")]
            public string Repository { get; set; }

            [JsonProperty("homepage")]
            public string Homepage { get; set; }

            [JsonProperty("documentation")]
            public string Documentation { get; set; }

            [JsonProperty("edition")]
            public string Edition { get; set; }

            [JsonProperty("links")]
            public object Links { get; set; }

            [JsonProperty("default_run")]
            public object DefaultRun { get; set; }

            [JsonProperty("rust_version")]
            public string RustVersion { get; set; }
        }
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public class Resolve
        {
            [JsonProperty("nodes")]
            public List<Node> Nodes { get; set; }

            [JsonProperty("root")]
            public string Root { get; set; }
        }
    }
}