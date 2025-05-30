// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    public abstract class JfrogApicommunication : IJFrogApiCommunication
    {
        protected string DomainName { get; set; }

        protected ArtifactoryCredentials ArtifactoryCredentials { get; set; }

        protected string SourceRepoName { get; set; }
        private static int TimeoutInSec { get; set; }

        protected JfrogApicommunication(string repoDomainName, string srcrepoName, ArtifactoryCredentials artifactoryCredentials, int timeout)
        {
            DomainName = repoDomainName;
            ArtifactoryCredentials = artifactoryCredentials;
            SourceRepoName = srcrepoName;
            TimeoutInSec = timeout;
        }

        private static HttpClient GetHttpClient(ArtifactoryCredentials credentials)
        {
            var handler = new RetryHttpClientHandler()
            {
                InnerHandler = new HttpClientHandler()
            };
            var httpClient = new HttpClient(handler);
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(TimeoutInSec);
            httpClient.Timeout = timeOutInSec;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.Token);
            return httpClient;
        }

        public async Task<HttpResponseMessage> DeletePackageFromJFrogRepo(string repoName, string componentName)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            httpClient.SetLogWarnings(false, "unable to delete package from jfrog repository");
            string url = $"{DomainName}/{repoName}/{componentName}";
            return await httpClient.DeleteAsync(url);
        }

        public abstract Task<HttpResponseMessage> GetApiKey();

        public abstract Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component);

        public abstract Task<HttpResponseMessage> CopyFromRemoteRepo(ComponentsToArtifactory component);

        public abstract Task<HttpResponseMessage> MoveFromRepo(ComponentsToArtifactory component);

        public abstract void UpdatePackagePropertiesInJfrog(string sw360releaseUrl, string destRepoName, UploadArgs uploadArgs);

    }
}
