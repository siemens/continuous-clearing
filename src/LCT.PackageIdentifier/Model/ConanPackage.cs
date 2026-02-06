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
        #region Properties
        /// <summary>
        /// Package name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Package version.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Context of the package (for example 'host' or 'build').
        /// </summary>
        [JsonProperty("context")]
        public string Context { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the package exposes libraries.
        /// </summary>
        [JsonProperty("libs")]
        public bool? Libs { get; set; }

        /// <summary>
        /// Visibility flag for the package.
        /// </summary>
        [JsonProperty("visible")]
        public bool? Visible { get; set; }

        /// <summary>
        /// Indicates whether the package is required at run time.
        /// </summary>
        [JsonProperty("run")]
        public bool? Run { get; set; }

        /// <summary>
        /// Indicates whether the package must be built.
        /// </summary>
        [JsonProperty("build")]
        public bool? Build { get; set; }

        /// <summary>
        /// Indicates whether the package is used for tests.
        /// </summary>
        [JsonProperty("test")]
        public bool? Test { get; set; }

        /// <summary>
        /// Dependency list keyed by node id as found in the Conan graph.
        /// </summary>
        [JsonProperty("dependencies")]
        public Dictionary<string, ConanDependency> Dependencies { get; set; } = new Dictionary<string, ConanDependency>();
        #endregion

        #region Methods
        /// <summary>
        /// Gets a value indicating whether this package should be treated as a runtime dependency.
        /// </summary>
        public bool IsRuntimeDependency => Context == "host" && Libs == true && Visible == true && Run == false;

        /// <summary>
        /// Gets a value indicating whether this package should be treated as a development dependency.
        /// </summary>
        public bool IsDevDependency => Context == "build" || (Run == true && Build == true);

        /// <summary>
        /// Determines whether the dependency with the specified node id is a direct dependency.
        /// </summary>
        /// <param name="nodeId">Node id to check within the Dependencies dictionary.</param>
        /// <returns>True if the dependency is present and marked as direct; otherwise false.</returns>
        public bool IsDirectDependency(string nodeId)
        {
            return Dependencies?.ContainsKey(nodeId) == true && Dependencies[nodeId].Direct == true;
        }
        #endregion

        #region Events
        #endregion
    }

    /// <summary>
    /// Represents the root structure of a Conan 2.0 dep.json file
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConanDepJson
    {
        #region Properties
        /// <summary>
        /// Top level graph element of the Conan dep.json.
        /// </summary>
        [JsonProperty("graph")]
        public ConanGraph Graph { get; set; }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion
    }

    /// <summary>
    /// Represents the graph section in a Conan 2.0 dep.json file
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConanGraph
    {
        #region Properties
        /// <summary>
        /// Nodes in the graph keyed by node id.
        /// </summary>
        [JsonProperty("nodes")]
        public Dictionary<string, ConanPackage> Nodes { get; set; } = new Dictionary<string, ConanPackage>();

        /// <summary>
        /// Root mapping information for the graph.
        /// </summary>
        [JsonProperty("root")]
        public Dictionary<string, string> Root { get; set; } = new Dictionary<string, string>();
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion
    }
}
