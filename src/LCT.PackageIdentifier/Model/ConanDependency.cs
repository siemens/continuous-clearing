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
        #region Properties
        /// <summary>
        /// Reference identifier for the Conan dependency (ref field).
        /// </summary>
        [JsonProperty("ref")]
        public string Ref { get; set; } = string.Empty;

        /// <summary>
        /// Requirement string for the dependency.
        /// </summary>
        [JsonProperty("require")]
        public string Require { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the dependency is required at run time.
        /// </summary>
        [JsonProperty("run")]
        public bool? Run { get; set; }

        /// <summary>
        /// Indicates whether the dependency exposes libraries.
        /// </summary>
        [JsonProperty("libs")]
        public bool? Libs { get; set; }

        /// <summary>
        /// Indicates whether the dependency should be skipped.
        /// </summary>
        [JsonProperty("skip")]
        public bool? Skip { get; set; }

        /// <summary>
        /// Indicates whether this dependency is used for tests.
        /// </summary>
        [JsonProperty("test")]
        public bool? Test { get; set; }

        /// <summary>
        /// Indicates whether this dependency should be forced.
        /// </summary>
        [JsonProperty("force")]
        public bool? Force { get; set; }

        /// <summary>
        /// Indicates whether the dependency is direct.
        /// </summary>
        [JsonProperty("direct")]
        public bool? Direct { get; set; }

        /// <summary>
        /// Indicates whether the dependency must be built.
        /// </summary>
        [JsonProperty("build")]
        public bool? Build { get; set; }

        /// <summary>
        /// Transitive headers information (type depends on dep.json structure).
        /// </summary>
        [JsonProperty("transitive_headers")]
        public object TransitiveHeaders { get; set; }

        /// <summary>
        /// Transitive libs information (type depends on dep.json structure).
        /// </summary>
        [JsonProperty("transitive_libs")]
        public object TransitiveLibs { get; set; }

        /// <summary>
        /// Indicates whether headers are included for this dependency.
        /// </summary>
        [JsonProperty("headers")]
        public bool? Headers { get; set; }

        /// <summary>
        /// Package id mode for this dependency.
        /// </summary>
        [JsonProperty("package_id_mode")]
        public string PackageIdMode { get; set; } = string.Empty;

        /// <summary>
        /// Visibility flag for the dependency.
        /// </summary>
        [JsonProperty("visible")]
        public bool? Visible { get; set; }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion
    }
}