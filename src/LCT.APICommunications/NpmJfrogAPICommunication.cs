// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models.Vulnerabilities;
using LCT.APICommunications.Model;
using LCT.Common;
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
        private static int TimeoutInSec { get; set; }
        public NpmJfrogApiCommunication(string repoDomainName, string srcrepoName, ArtifactoryCredentials repoCredentials, int timeout) : base(repoDomainName, srcrepoName, repoCredentials, timeout)
        {
            TimeoutInSec = timeout;
        }

        private static HttpClient GetHttpClient(ArtifactoryCredentials credentials)
        {
            HttpClient httpClient = new HttpClient();
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(TimeoutInSec);
            httpClient.Timeout = timeOutInSec;
            httpClient.DefaultRequestHeaders.Add(ApiConstant.JFrog_API_Header, credentials.ApiKey);
            httpClient.DefaultRequestHeaders.Add(ApiConstant.Email, credentials.Email);
            return httpClient;
        }

        public override async Task<HttpResponseMessage> GetApiKey()
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            string url = $"{DomainName}/api/security/apiKey";
            HttpResponseMessage responseMessage=new HttpResponseMessage();
            try
            {
                responseMessage= await httpClient.GetAsync(url);
                responseMessage.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                LogExceptionHandling.HttpException(ex, responseMessage, "JFROG");
            }
            catch (AggregateException ex)
            {
                LogExceptionHandling.AggregateException(ex, "JFROG");
            }
            return responseMessage;
        }

        public override async Task<HttpResponseMessage> CopyFromRemoteRepo(ComponentsToArtifactory component)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            HttpResponseMessage responseMessage = new HttpResponseMessage();
            try
            {
                responseMessage= await httpClient.PostAsync(component.CopyPackageApiUrl, httpContent);
                responseMessage.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                LogExceptionHandling.HttpException(ex, responseMessage, "JFROG");
            }
            catch (AggregateException ex)
            {
                LogExceptionHandling.AggregateException(ex, "JFROG");
            }
            return responseMessage;
        }

        public override async Task<HttpResponseMessage> MoveFromRepo(ComponentsToArtifactory component)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            HttpResponseMessage responseMessage = new HttpResponseMessage();
            try
            {
                responseMessage= await httpClient.PostAsync(component.MovePackageApiUrl, httpContent);
                responseMessage.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                LogExceptionHandling.HttpException(ex, responseMessage, "JFROG");
            }
            catch (AggregateException ex)
            {
                LogExceptionHandling.AggregateException(ex, "JFROG");
            }
            return responseMessage;
        }

        public override async Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();
            try
            {
                HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
                responseMessage = await httpClient.GetAsync(component.PackageInfoApiUrl);
            }
            catch (HttpRequestException ex)
            {
                LogExceptionHandling.HttpException(ex, responseMessage, "JFROG");
            }
            catch (AggregateException ex)
            {
                LogExceptionHandling.AggregateException(ex, "JFROG");
            }
            return responseMessage;
        }

        public override async void UpdatePackagePropertiesInJfrog(string sw360releaseUrl, string destRepoName, UploadArgs uploadArgs)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            HttpResponseMessage responseMessage = new HttpResponseMessage();    
            string url = $"{DomainName}/api/storage/{destRepoName}/{uploadArgs.PackageName}/-/{uploadArgs.ReleaseName}-{uploadArgs.Version}.tgz?" +
              $"properties=sw360url={sw360releaseUrl}";
            try
            {
                responseMessage=await httpClient.PutAsync(url, httpContent);
            }
            catch (HttpRequestException ex)
            {
                LogExceptionHandling.HttpException(ex, responseMessage, "JFROG");
            }
            catch (AggregateException ex)
            {
                LogExceptionHandling.AggregateException(ex, "JFROG");
            }

        }
    }
}
