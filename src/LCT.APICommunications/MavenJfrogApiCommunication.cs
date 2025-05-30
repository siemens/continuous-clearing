// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    public class MavenJfrogApiCommunication(string repoDomainName, string srcrepoName, ArtifactoryCredentials repoCredentials, int timeout) : JfrogApicommunication(repoDomainName, srcrepoName, repoCredentials, timeout)
    {
        public override async Task<HttpResponseMessage> GetApiKey()
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            string url = $"{DomainName}/api/security/apiKey";
            return await httpClient.GetAsync(url);
        }

        public override async Task<HttpResponseMessage> CopyFromRemoteRepo(ComponentsToArtifactory component, string correlationId)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            await LogHandlingHelper.HttpRequestHandling("Package copy from remote repository", $"MethodName:CopyFromRemoteRepo(),CorrelationId:{correlationId}", httpClient, component.CopyPackageApiUrl, httpContent);
            return await httpClient.PostAsync(component.CopyPackageApiUrl, httpContent);
        }

        public override async Task<HttpResponseMessage> MoveFromRepo(ComponentsToArtifactory component, string correlationId)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            await LogHandlingHelper.HttpRequestHandling("Package Move from remote repository", $"MethodName:MoveFromRepo(),CorrelationId:{correlationId}", httpClient, component.MovePackageApiUrl, httpContent);
            return await httpClient.PostAsync(component.MovePackageApiUrl, httpContent);
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
            string url = $"{DomainName}/api/storage/{destRepoName}/{uploadArgs.PackageName}/{uploadArgs.ReleaseName}/{uploadArgs.Version}/{uploadArgs.ReleaseName}.{uploadArgs.Version}-sources.jar?" +
                 $"properties=sw360url={sw360releaseUrl}";
            httpClient.PutAsync(url, httpContent);
        }
    }
}
