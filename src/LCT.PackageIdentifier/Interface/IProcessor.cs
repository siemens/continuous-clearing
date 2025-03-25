// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier.Interface
{
    public interface IProcessor
    {
        public Task<List<Component>> CheckInternalComponentsInJfrogArtifactory(CommonAppSettings appSettings, ArtifactoryCredentials artifactoryUpload, Component component, string repo);
        public Task<List<Component>> GetJfrogArtifactoryRepoInfo(CommonAppSettings appSettings, ArtifactoryCredentials artifactoryUpload, Component component, string repo);

    }
}
