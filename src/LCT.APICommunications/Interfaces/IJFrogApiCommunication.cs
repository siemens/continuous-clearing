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
    public interface IJFrogApiCommunication
    {
        Task<HttpResponseMessage> GetPackageByPackageName(UploadArgs uploadArgs);
        Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component);
        Task<HttpResponseMessage> DeletePackageFromJFrogRepo(string repoName, string componentName);
        Task<HttpResponseMessage> CopyPackageFromRemoteRepo(UploadArgs uploadArgs, string destreponame);
        Task<HttpResponseMessage> CopyFromRemoteRepo(ComponentsToArtifactory component);
        Task<HttpResponseMessage> GetApiKey();
        Task<HttpResponseMessage> CheckPackageAvailabilityInRepo(string repoName, string componentName, string componentVersion);
        void UpdatePackagePropertiesInJfrog(string sw360releaseUrl, string destRepoName, UploadArgs uploadArgs);

    }
}
