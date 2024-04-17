// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using LCT.APICommunications.Model;
using System.Collections.Generic;

namespace LCT.ArtifactoryUploader.Model
{
    /// <summary>
    /// The Model class for UnkmownPackagesAll
    /// </summary>
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
}
