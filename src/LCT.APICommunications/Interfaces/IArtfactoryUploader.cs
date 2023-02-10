// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 
using LCT.APICommunications.Model;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.APICommunications.Interfaces
{
    public interface IArtfactoryUploader
    {
        Task<HttpResponseMessage> UploadNPMPackageToArtifactory(string sw360ReleaseId, string sw360releaseUrl, ArtifactoryCredentials credentials);

        Task<HttpResponseMessage> UploadNUGETPackageToArtifactory(string sw360ReleaseId, string sw360releaseUrl, ArtifactoryCredentials credentials);
        Task<HttpResponseMessage> UploadPackageToRepo(ComponentsToArtifactory component);
    }
}
