// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.APICommunications.Interfaces
{
    public interface IJFrogApiCommunication
    {
        Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component);

        Task<HttpResponseMessage> DeletePackageFromJFrogRepo(string repoName, string componentName);

        Task<HttpResponseMessage> CopyFromRemoteRepo(ComponentsToArtifactory component);

        Task<HttpResponseMessage> MoveFromRepo(ComponentsToArtifactory component);

        Task<HttpResponseMessage> GetApiKey();

        void UpdatePackagePropertiesInJfrog(string sw360releaseUrl, string destRepoName, UploadArgs uploadArgs);
    }
}
