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
        #region Properties
        /// <summary>
        /// List of packages returned by the cargo metadata output.
        /// </summary>
        [JsonProperty("packages")]
        public List<Package> Packages { get; set; }

        /// <summary>
        /// Dependency resolve information including nodes and root.
        /// </summary>
        [JsonProperty("resolve")]
        public Resolve ResolveInfo { get; set; }

        /// <summary>
        /// Workspace members identifiers.
        /// </summary>
        [JsonProperty("workspace_members")]
        public List<string> Workspace_members { get; set; }

        /// <summary>
        /// Default workspace members identifiers.
        /// </summary>
        [JsonProperty("workspace_default_members")]
        public List<string> Workspace_default_members { get; set; }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public class Dep
        {
            #region Properties
            /// <summary>
            /// Dependency name.
            /// </summary>
            [JsonProperty("name")]
            public string Name { get; set; }

            /// <summary>
            /// Package id reference for the dependency.
            /// </summary>
            [JsonProperty("pkg")]
            public string Pkg { get; set; }

            /// <summary>
            /// Kinds of dependency entries (e.g., normal, dev).
            /// </summary>
            [JsonProperty("dep_kinds")]
            public List<DepKind> DepKinds { get; set; }
            #endregion
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public class DepKind
        {
            #region Properties
            /// <summary>
            /// Kind of dependency (for example "dev" or "normal").
            /// </summary>
            [JsonProperty("kind")]
            public string Kind { get; set; }

            /// <summary>
            /// Target platform or crate for the dependency kind.
            /// </summary>
            [JsonProperty("target")]
            public string Target { get; set; }
            #endregion
        }
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public class Node
        {
            #region Properties
            /// <summary>
            /// Node identifier in the resolve graph.
            /// </summary>
            [JsonProperty("id")]
            public string Id { get; set; }

            /// <summary>
            /// List of dependency ids for this node.
            /// </summary>
            [JsonProperty("dependencies")]
            public List<string> Dependencies { get; set; }

            /// <summary>
            /// Detailed dependency entries for this node.
            /// </summary>
            [JsonProperty("deps")]
            public List<Dep> Deps { get; set; }

            /// <summary>
            /// Enabled feature flags for this node.
            /// </summary>
            [JsonProperty("features")]
            public List<string> Features { get; set; }
            #endregion
        }
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public class Package
        {
            #region Properties
            /// <summary>
            /// Package name.
            /// </summary>
            [JsonProperty("name")]
            public string Name { get; set; }

            /// <summary>
            /// Package version.
            /// </summary>
            [JsonProperty("version")]
            public string Version { get; set; }

            /// <summary>
            /// Package id (unique within the metadata output).
            /// </summary>
            [JsonProperty("id")]
            public string Id { get; set; }

            /// <summary>
            /// Declared license for the package.
            /// </summary>
            [JsonProperty("license")]
            public string License { get; set; }

            /// <summary>
            /// License file path or details if provided.
            /// </summary>
            [JsonProperty("license_file")]
            public object LicenseFile { get; set; }

            /// <summary>
            /// Package description.
            /// </summary>
            [JsonProperty("description")]
            public string Description { get; set; }

            /// <summary>
            /// Source information, e.g., git repository reference.
            /// </summary>
            [JsonProperty("source")]
            public string Source { get; set; }

            /// <summary>
            /// Path to the Cargo.toml manifest for the package.
            /// </summary>
            [JsonProperty("manifest_path")]
            public string ManifestPath { get; set; }

            /// <summary>
            /// Publish information if present.
            /// </summary>
            [JsonProperty("publish")]
            public object Publish { get; set; }

            /// <summary>
            /// List of package authors.
            /// </summary>
            [JsonProperty("authors")]
            public List<string> Authors { get; set; }

            /// <summary>
            /// Package categories.
            /// </summary>
            [JsonProperty("categories")]
            public List<string> Categories { get; set; }

            /// <summary>
            /// Keywords associated with the package.
            /// </summary>
            [JsonProperty("keywords")]
            public List<string> Keywords { get; set; }

            /// <summary>
            /// Readme content or path.
            /// </summary>
            [JsonProperty("readme")]
            public string Readme { get; set; }

            /// <summary>
            /// Repository URL for the package.
            /// </summary>
            [JsonProperty("repository")]
            public string Repository { get; set; }

            /// <summary>
            /// Homepage URL for the package.
            /// </summary>
            [JsonProperty("homepage")]
            public string Homepage { get; set; }

            /// <summary>
            /// Documentation URL for the package.
            /// </summary>
            [JsonProperty("documentation")]
            public string Documentation { get; set; }

            /// <summary>
            /// Edition of Rust used by the package.
            /// </summary>
            [JsonProperty("edition")]
            public string Edition { get; set; }

            /// <summary>
            /// Additional links information.
            /// </summary>
            [JsonProperty("links")]
            public object Links { get; set; }

            /// <summary>
            /// Default run information if available.
            /// </summary>
            [JsonProperty("default_run")]
            public object DefaultRun { get; set; }

            /// <summary>
            /// Rust toolchain version requirement.
            /// </summary>
            [JsonProperty("rust_version")]
            public string RustVersion { get; set; }
            #endregion
        }
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public class Resolve
        {
            #region Properties
            /// <summary>
            /// Nodes that make up the resolved dependency graph.
            /// </summary>
            [JsonProperty("nodes")]
            public List<Node> Nodes { get; set; }

            /// <summary>
            /// Root package id in the resolve graph.
            /// </summary>
            [JsonProperty("root")]
            public string Root { get; set; }
            #endregion
        }
    }
}