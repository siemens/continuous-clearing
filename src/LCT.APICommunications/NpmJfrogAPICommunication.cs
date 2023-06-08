// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using log4net;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    public class NpmJfrogApiCommunication : JfrogApicommunication
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static int timeoutsec { get; set; }
        public NpmJfrogApiCommunication(string repoDomainName, string srcrepoName, ArtifactoryCredentials repoCredentials, int timeout) : base(repoDomainName, srcrepoName, repoCredentials, timeout)
        {
        }

        private static HttpClient GetHttpClient(ArtifactoryCredentials credentials)
        {
            HttpClient httpClient = new HttpClient();
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(timeoutsec);
            httpClient.Timeout = timeOutInSec;
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

        public override async Task<HttpResponseMessage> CopyPackageFromRemoteRepo(UploadArgs uploadArgs, string destreponame)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            string url = $"{DomainName}/api/copy/{SourceRepoName}/{uploadArgs.PackageName}/-/{uploadArgs.ReleaseName}-{uploadArgs.Version}" +
              $".tgz?to=/{destreponame}/{uploadArgs.PackageName}/-/{uploadArgs.ReleaseName}-{uploadArgs.Version}.tgz";
            return await httpClient.PostAsync(url, httpContent);
        }

        public override async Task<HttpResponseMessage> CopyFromRemoteRepo(ComponentsToArtifactory component)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            return await httpClient.PostAsync(component.CopyPackageApiUrl, httpContent);
        }

        public override async Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();
            var result = responseMessage;
            try
            {
                HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
                result = await httpClient.GetAsync(component.PackageInfoApiUrl);
            }
            catch (TaskCanceledException ex)
            {
                Logger.Debug($"{ex.Message}");
                Logger.Error("A timeout error is thrown from Jfrog server,Please wait for sometime and re run the pipeline again");
                Environment.Exit(-1);

            }
            return result;
        }

        public override async Task<HttpResponseMessage> GetPackageByPackageName(UploadArgs uploadArgs)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();
            var result = responseMessage;
            try
            {
                HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
                string url = $"{DomainName}/api/storage/{SourceRepoName}/{uploadArgs.PackageName}/-/{uploadArgs.ReleaseName}-{uploadArgs.Version}.tgz";
                result = await httpClient.GetAsync(url);
            }
            catch (HttpRequestException ex)
            {
                Logger.Debug($"{ex.Message}");
                Logger.Error("Connection issue with Jfrog artifactory!Please rerun the pipeline");
                Environment.Exit(-1);
            }
            catch (TaskCanceledException ex)
            {
                Logger.Debug($"{ex.Message}");
                Logger.Error("A timeout error is thrown from Jfrog server,Please wait for sometime and re run the pipeline again");
                Environment.Exit(-1);

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
