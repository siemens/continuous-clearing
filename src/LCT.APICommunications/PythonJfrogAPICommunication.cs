// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    public class PythonJfrogApiCommunication : JfrogApicommunication
    {
        public PythonJfrogApiCommunication(string repoDomainName, string srcrepoName, ArtifactoryCredentials repoCredentials, int timeout) : base(repoDomainName, srcrepoName, repoCredentials, timeout)
        {
        }
        private static HttpClient GetHttpClient(ArtifactoryCredentials credentials)
        {
            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add(ApiConstant.JFrog_API_Header, credentials.ApiKey);
            httpClient.DefaultRequestHeaders.Add(ApiConstant.Email, credentials.Email);
            return httpClient;
        }

        public override async Task<HttpResponseMessage> GetApiKey()
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            string url = $"{DomainName}/api/security/apiKey";
            return await httpClient.GetAsync(url);
        }

        public override async Task<HttpResponseMessage> CopyFromRemoteRepo(ComponentsToArtifactory component)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            return await httpClient.PostAsync(component.CopyPackageApiUrl, httpContent);
        }
        public override async Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            return await httpClient.GetAsync(component.PackageInfoApiUrl);
        }
        public override void UpdatePackagePropertiesInJfrog(string sw360releaseUrl, string destRepoName, UploadArgs uploadArgs)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            string url = $"{DomainName}/api/storage/{destRepoName}/{uploadArgs.ReleaseName}.{uploadArgs.Version}.pypi?" +
                 $"properties=sw360url={sw360releaseUrl}";
            httpClient.PutAsync(url, httpContent);
        }

    }
}


