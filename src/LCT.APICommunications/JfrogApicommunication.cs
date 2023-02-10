// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 
using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    public abstract class JfrogApicommunication : IJFrogApiCommunication
    {
        protected string DomainName { get; set; }
        protected ArtifactoryCredentials ArtifactoryCredentials { get; set; }
        protected string SourceRepoName { get; set; }
        protected JfrogApicommunication(string repoDomainName, string srcrepoName, ArtifactoryCredentials artifactoryCredentials)
        {
            DomainName = repoDomainName;
            ArtifactoryCredentials = artifactoryCredentials;
            SourceRepoName = srcrepoName;
        }
        private static HttpClient GetHttpClient(ArtifactoryCredentials credentials)
        {
            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add(ApiConstant.JFrog_API_Header, credentials.ApiKey);
            httpClient.DefaultRequestHeaders.Add(ApiConstant.Email, credentials.Email);
            return httpClient;
        }

        public async Task<HttpResponseMessage> DeletePackageFromJFrogRepo(string repoName, string componentName)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            string url = $"{DomainName}/{repoName}/{componentName}";
            return await httpClient.DeleteAsync(url);
        }
        public abstract Task<HttpResponseMessage> GetApiKey();

        public abstract Task<HttpResponseMessage> GetPackageByPackageName(UploadArgs uploadArgs);
        public abstract Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component);
        public abstract Task<HttpResponseMessage> CopyPackageFromRemoteRepo(UploadArgs uploadArgs, string destreponame);
        public abstract Task<HttpResponseMessage> CopyFromRemoteRepo(ComponentsToArtifactory component);
        public abstract Task<HttpResponseMessage> CheckPackageAvailabilityInRepo(string repoName, string componentName, string componentVersion);
        public abstract void UpdatePackagePropertiesInJfrog(string sw360releaseUrl, string destRepoName, UploadArgs uploadArgs);

    }
}
