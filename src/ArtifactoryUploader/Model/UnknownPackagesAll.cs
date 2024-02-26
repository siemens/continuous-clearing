// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using LCT.APICommunications.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.ArtifactoryUploader.Model
{
    /// <summary>
    /// The Model class for UnkmownPackagesAll
    /// </summary>
    public class UnknownPackagesAll
    {
        public List<ComponentsToArtifactory> UnknownPackagesNpm { get; set; }
        public List<ComponentsToArtifactory> UnknownPackagesNuget { get; set; }
        public List<ComponentsToArtifactory> UnknownPackagesConan { get; set; }
        public List<ComponentsToArtifactory> UnknownPackagesPython { get; set; }
        public List<ComponentsToArtifactory> UnknownPackagesDebian { get; set; }
        public List<ComponentsToArtifactory> UnknownPackagesMaven { get; set; }

    }
}
