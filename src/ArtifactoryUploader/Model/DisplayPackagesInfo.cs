// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using LCT.APICommunications.Model;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.ArtifactoryUploader.Model
{
    /// <summary>
    /// The Model class for DisplayPackagesInfo
    /// </summary>

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DisplayPackagesInfo
    {
        public List<ComponentsToArtifactory> UnknownPackagesNpm { get; set; }
        public List<ComponentsToArtifactory> UnknownPackagesNuget { get; set; }
        public List<ComponentsToArtifactory> UnknownPackagesConan { get; set; }
        public List<ComponentsToArtifactory> UnknownPackagesPython { get; set; }
        public List<ComponentsToArtifactory> UnknownPackagesDebian { get; set; }
        public List<ComponentsToArtifactory> UnknownPackagesMaven { get; set; }
        public List<ComponentsToArtifactory> JfrogNotFoundPackagesNpm { get; set; }
        public List<ComponentsToArtifactory> JfrogNotFoundPackagesNuget { get; set; }
        public List<ComponentsToArtifactory> JfrogNotFoundPackagesConan { get; set; }
        public List<ComponentsToArtifactory> JfrogNotFoundPackagesPython { get; set; }
        public List<ComponentsToArtifactory> JfrogNotFoundPackagesDebian { get; set; }
        public List<ComponentsToArtifactory> JfrogNotFoundPackagesMaven { get; set; }
        public List<ComponentsToArtifactory> JfrogFoundPackagesNpm { get; set; }
        public List<ComponentsToArtifactory> JfrogFoundPackagesNuget { get; set; }
        public List<ComponentsToArtifactory> JfrogFoundPackagesConan { get; set; }
        public List<ComponentsToArtifactory> JfrogFoundPackagesPython { get; set; }
        public List<ComponentsToArtifactory> JfrogFoundPackagesDebian { get; set; }
        public List<ComponentsToArtifactory> JfrogFoundPackagesMaven { get; set; }
        public List<ComponentsToArtifactory> SuccessfullPackagesNpm { get; set; }
        public List<ComponentsToArtifactory> SuccessfullPackagesNuget { get; set; }
        public List<ComponentsToArtifactory> SuccessfullPackagesConan { get; set; }
        public List<ComponentsToArtifactory> SuccessfullPackagesPython { get; set; }
        public List<ComponentsToArtifactory> SuccessfullPackagesDebian { get; set; }
        public List<ComponentsToArtifactory> SuccessfullPackagesMaven { get; set; }

    }
    public class ProjectResponse
    {
        [JsonProperty("npm")]
        public List<JsonComponents> Npm { get; set; }
        [JsonProperty("nuget")]
        public List<JsonComponents> Nuget { get; set; }
        [JsonProperty("conan")]
        public List<JsonComponents> Conan { get; set; }
        [JsonProperty("poetry")]
        public List<JsonComponents> Python { get; set; }
        [JsonProperty("debian")]
        public List<JsonComponents> Debian { get; set; }
        [JsonProperty("maven")]
        public List<JsonComponents> Maven { get; set; }

    }

    public class JsonComponents
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
