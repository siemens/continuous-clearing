// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Interface;
using LCT.Common.Logging;
using log4net;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    public class NpmJfrogApiCommunication(string repoDomainName, string srcrepoName, ArtifactoryCredentials repoCredentials, int timeout) : JfrogApicommunication(repoDomainName, srcrepoName, repoCredentials, timeout)
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
       
        private static IEnvironmentHelper environmentHelper = new EnvironmentHelper();

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
            LogHandling.LogRequestDetails("Package Copy from remote repository", $"MethodName:CopyFromRemoteRepo(),CorrelationId:{correlationId}", httpClient, component.MovePackageApiUrl, httpContent);
            return await httpClient.PostAsync(component.CopyPackageApiUrl, httpContent);
        }

        public override async Task<HttpResponseMessage> MoveFromRepo(ComponentsToArtifactory component, string correlationId)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            LogHandling.LogRequestDetails("Package Move from remote repository", $"MethodName:CopyFromRemoteRepo(),CorrelationId:{correlationId}", httpClient, component.MovePackageApiUrl, httpContent);
            return await httpClient.PostAsync(component.MovePackageApiUrl, httpContent);
        }

        public override async Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();
            var result = responseMessage;
            try
            {
                HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
                result = await httpClient.GetAsync(component.PackageInfoApiUrl);
                result.EnsureSuccessStatusCode();
            }
            catch (TaskCanceledException ex)
            {
                Logger.Debug($"{ex.Message}");
                ExceptionHandling.TaskCancelledException(ex, "Jfrog");
                environmentHelper.CallEnvironmentExit(-1);
            }
            return result;
        }

        public override void UpdatePackagePropertiesInJfrog(string sw360releaseUrl, string destRepoName, UploadArgs uploadArgs)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            string url = $"{DomainName}/api/storage/{destRepoName}/{uploadArgs.PackageName}/-/{uploadArgs.ReleaseName}-{uploadArgs.Version}.tgz?" +
              $"properties=sw360url={sw360releaseUrl}";
            httpClient.PutAsync(url, httpContent);
        }
    }
}
