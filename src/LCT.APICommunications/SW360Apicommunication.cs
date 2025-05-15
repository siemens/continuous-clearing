// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Interface;
using LCT.Common.Logging;
using LCT.Common.Model;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    public static class HttpClientExtensions
    {
        public static void SetLogWarnings(this HttpClient client, bool logWarnings)
        {
            client.DefaultRequestHeaders.Add("LogWarnings", logWarnings.ToString());
        }
    }
    /// <summary>
    /// Communicatest with SW360 API
    /// </summary>
    public class SW360Apicommunication : ISw360ApiCommunication
    {
        #region VARIABLE DECLARATION
        private readonly string sw360AuthTokenType;
        private readonly string sw360AuthToken;
        private readonly string sw360ComponentApi;
        private readonly string sw360ReleaseApi;
        private readonly string sw360ProjectsApi;
        private readonly string sw360ReleaseNameApi;
        private readonly string sw360ProjectByTagApi;
        private readonly string sw360ReleaseByExternalId;
        private readonly string sw360ComponentByExternalId;
        private readonly string sw360UsersApi;
        private readonly int timeOut;
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static IEnvironmentHelper environmentHelper = new EnvironmentHelper();
        #endregion

        #region PUBLIC METHODS
        public SW360Apicommunication(SW360ConnectionSettings sw360ConnectionSettings)
        {
            sw360AuthTokenType = sw360ConnectionSettings.SW360AuthTokenType;
            sw360AuthToken = sw360ConnectionSettings.Sw360Token;
            timeOut = sw360ConnectionSettings.Timeout;
            sw360ComponentApi = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360ComponentApiSuffix}";
            sw360ReleaseApi = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360ReleaseApiSuffix}";
            sw360ProjectsApi = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360ProjectsApiSuffix}";
            sw360ReleaseNameApi = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360ReleaseNameApiSuffix}";
            sw360ProjectByTagApi = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360ProjectByTagApiSuffix}";
            sw360ComponentByExternalId = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360ComponentByExternalId}";
            sw360ReleaseByExternalId = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360ReleaseByExternalId}";
            sw360UsersApi = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360UsersSuffix}";
        }


        public async Task<string> GetProjects()
        {
            HttpClient httpClient = GetHttpClient();
            var result = string.Empty;
            try
            {
                result = await httpClient.GetStringAsync(sw360ProjectsApi);
            }
            catch (TaskCanceledException ex)
            {
                Logger.Debug($"{ex.Message}");
                Logger.Error("A timeout error is thrown from SW360 server,Please wait for sometime and re run the pipeline again");
                environmentHelper.CallEnvironmentExit(-1);
            }
            return result;
        }

        public async Task<string> GetSw360Users()
        {
            HttpClient httpClient = GetHttpClient();
            return await httpClient.GetStringAsync(sw360UsersApi);
        }

        public async Task<string> GetProjectsByName(string projectName)
        {
            HttpClient httpClient = GetHttpClient();
            string projectNameApiUrl = $"{sw360ProjectsApi}{ApiConstant.ComponentNameUrl}{projectName}";
            return await httpClient.GetStringAsync(projectNameApiUrl);
        }

        public async Task<HttpResponseMessage> GetProjectsByTag(string projectTag)
        {
            HttpClient httpClient = GetHttpClient();
            string projectsByTagUrl = $"{sw360ProjectByTagApi}{projectTag}";
            return await httpClient.GetAsync(projectsByTagUrl);
        }

        public async Task<HttpResponseMessage> GetProjectById(string projectId)
        {
            HttpClient httpClient = GetHttpClient();
            HttpResponseMessage obj = new HttpResponseMessage();
            var result = obj;
            string projectsByTagUrl = $"{sw360ProjectsApi}/{projectId}";
            string correlationId = Guid.NewGuid().ToString();
            LogHandling.LogRequestDetails("Get sw360 Project details for validating", $"MethodName:GetProjectById(),CorrelationId:{correlationId}", httpClient, projectsByTagUrl);

            try
            {
                result = await httpClient.GetAsync(projectsByTagUrl);
                LogHandling.LogHttpResponseDetails("Get sw360 Project details for validating", $"MethodName:GetProjectById(),CorrelationId:{correlationId}", result, "");
                result.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                LogHandling.HttpErrorHandelingForLog("Get sw360 Project details for validating", $"MethodName:GetProjectById(),CorrelationId:{correlationId}", ex, "");
                ExceptionHandling.HttpException(ex, result, "SW360");
                environmentHelper.CallEnvironmentExit(-1);
            }
            catch (TaskCanceledException ex)
            {
                LogHandling.HttpErrorHandelingForLog("Get sw360 Project details for validating", $"MethodName:GetProjectById(),CorrelationId:{correlationId}", ex, "");
                ExceptionHandling.TaskCancelledException(ex, "SW360");
                environmentHelper.CallEnvironmentExit(-1);
            }
            return result;
        }

        public async Task<string> GetReleases()
        {
            HttpClient httpClient = GetHttpClient();
            var result = string.Empty;
            string correlationId = Guid.NewGuid().ToString();
            try
            {
                LogHandling.LogRequestDetails("Request for get all releases", $"MethodName:GetReleases(),CorrelationId:{correlationId}", httpClient, sw360ReleaseApi);
                HttpResponseMessage responseMessage = await httpClient.GetAsync(sw360ReleaseApi);
                LogHandling.LogHttpResponseDetails("Response of get all releases", $"MethodName:GetReleases(),CorrelationId:{correlationId}", responseMessage);
                if (responseMessage != null && responseMessage.StatusCode.Equals(HttpStatusCode.OK))
                {
                    return await responseMessage.Content.ReadAsStringAsync();
                }
                else
                {
                    LogHandling.BasicErrorHandelingForLog("GetReleases", $"MethodName:GetReleases(),CorrelationId:{correlationId}",
                $"SW360 server is not accessible. StatusCode: {responseMessage?.StatusCode}, ReasonPhrase: {responseMessage?.ReasonPhrase}",
                "Please wait for some time and re-run the pipeline.");
                    Logger.Error("SW360 server is not accessible while getting All Releases,Please wait for sometime and re run the pipeline again." +
                        " StatusCode:" + responseMessage?.StatusCode + " & ReasonPharse :" + responseMessage?.ReasonPhrase);
                    environmentHelper.CallEnvironmentExit(-1);
                }
            }
            catch (TaskCanceledException ex)
            {
                LogHandling.HttpErrorHandelingForLog("GetReleases", $"MethodName:GetReleases(),CorrelationId:{correlationId}", ex,
            "TaskCanceledException occurred while getting all releases from the SW360 server. Please wait for some time and re-run the pipeline.");
                Logger.Error("TaskCanceledException error has error while getting all releases from the SW360 server,Please wait for sometime and re run the pipeline again. Error :" + ex.Message);
                environmentHelper.CallEnvironmentExit(-1);
            }
            catch (HttpRequestException ex)
            {
                LogHandling.HttpErrorHandelingForLog("GetReleases", $"MethodName:GetReleases(),CorrelationId:{correlationId}", ex,"");
                Logger.Error("HttpRequestException error has error while getting all releases from the SW360 server,Please wait for sometime and re run the pipeline again. Error :" + ex.Message);
                environmentHelper.CallEnvironmentExit(-1);
            }
            catch (InvalidOperationException ex)
            {
                LogHandling.HttpErrorHandelingForLog("GetReleases", $"MethodName:GetReleases(),CorrelationId:{correlationId}", ex,
            "InvalidOperationException occurred while getting all releases from the SW360 server. Please wait for some time and re-run the pipeline.");
                Logger.Error("InvalidOperationException error has error while getting all releases from the SW360 server,Please wait for sometime and re run the pipeline again. Error :" + ex.Message);
                environmentHelper.CallEnvironmentExit(-1);
            }
            return result;
        }
        public async Task<string> TriggerFossologyProcess(string releaseId, string sw360link)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false);
            string url = $"{sw360ReleaseApi}/{releaseId}{ApiConstant.FossTriggerAPIPrefix}{sw360link}{ApiConstant.FossTriggerAPISuffix}";
            string correlationId = Guid.NewGuid().ToString();
            try
            {
                LogHandling.LogRequestDetails("TriggerFossologyProcess",$"MethodName:TriggerFossologyProcess(),correlationId:{correlationId}",httpClient,url);
                var response = await httpClient.GetAsync(url);
                LogHandling.LogHttpResponseDetails("TriggerFossologyProcess",$"MethodName:TriggerFossologyProcess(),correlationId:{correlationId}",response);
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    var errorDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(errorContent);
                    string message = errorDetails.TryGetValue("message", out object value) ? value.ToString() : "Error";
                    int status = (int)response.StatusCode;
                    throw new HttpRequestException($"{status}:{message}");
                }
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                LogHandling.HttpErrorHandelingForLog("TriggerFossologyProcess",$"MethodName:TriggerFossologyProcess(),correlationId:{correlationId}",ex,"An HTTP request error occurred while triggering the Fossology process.");
                throw;
            }
        }
        public async Task<HttpResponseMessage> CheckFossologyProcessStatus(string link,string correlationId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false);
            LogHandling.LogRequestDetails("TriggerFossologyProcess", $"MethodName:TriggerFossologyProcess(),correlationId:{correlationId}", httpClient, link);
            return await httpClient.GetAsync(link);
        }
        public async Task<string> GetComponents(string correlationId)
        {
            HttpClient httpClient = GetHttpClient();
            LogHandling.LogRequestDetails("Request for get components data", $"MethodName:GetComponents(),CorrelationId:{correlationId}", httpClient, sw360ComponentApi);
            return await httpClient.GetStringAsync(sw360ComponentApi);
        }

        public async Task<HttpResponseMessage> GetReleaseByExternalId(string purlId, string externalIdKey = "",string correlationId="")
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false);
            string releaseByExternalIdUrl = $"{sw360ReleaseByExternalId}{externalIdKey}{purlId}";
            LogHandling.LogRequestDetails("Request for get release data by ExternalId", $"MethodName:GetReleaseByExternalId(),CorrelationId:{correlationId}", httpClient, releaseByExternalIdUrl);
            return await httpClient.GetAsync(releaseByExternalIdUrl);
        }

        public async Task<HttpResponseMessage> GetComponentByExternalId(string purlId, string externalIdKey = "", string correlationId = "")
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false);
            string componentByExternalIdUrl = $"{sw360ComponentByExternalId}{externalIdKey}{purlId}";
            LogHandling.LogRequestDetails("Request for get component data by ExternalId", $"MethodName:GetComponentByExternalId(),CorrelationId:{correlationId}", httpClient, componentByExternalIdUrl);
            return await httpClient.GetAsync(componentByExternalIdUrl);
        }

        public async Task<HttpResponseMessage> GetReleaseById(string releaseId,string correlationId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false);
            string url = $"{sw360ReleaseApi}/{releaseId}";
            LogHandling.LogRequestDetails("Request for get release data by ReleaseId", $"MethodName:GetReleaseById(),CorrelationId:{correlationId}", httpClient, url);
            return await httpClient.GetAsync(url);
        }

        public async Task<HttpResponseMessage> GetReleaseByLink(string releaseLink)
        {
            HttpClient httpClient = GetHttpClient();
            return await httpClient.GetAsync(releaseLink);
        }

        public async Task<HttpResponseMessage> LinkReleasesToProject(HttpContent httpContent, string sw360ProjectId,string correlationId)
        {
            HttpClient httpClient = GetHttpClient();
            string url = $"{sw360ProjectsApi}/{sw360ProjectId}/{ApiConstant.Releases}";
            LogHandling.LogRequestDetails("LinkReleasesToProject", $"MethodName:LinkReleasesToProject(), CorrelationId: {correlationId}, ProjectId: {sw360ProjectId}", httpClient, url, httpContent);
            return await httpClient.PostAsync(url, httpContent);
        }

        public async Task<HttpResponseMessage> UpdateLinkedRelease(string projectId, string releaseId, UpdateLinkedRelease updateLinkedRelease)
        {
            HttpClient httpClient = GetHttpClient();
            string updateUri = $"{sw360ProjectsApi}/{projectId}/{ApiConstant.Release}/{releaseId}";
            string updateContent = JsonConvert.SerializeObject(updateLinkedRelease);
            HttpContent content = new StringContent(updateContent, Encoding.UTF8, "application/json");

            return await httpClient.PatchAsync(updateUri, content);
        }

        public async Task<HttpResponseMessage> CreateComponent(CreateComponent createComponentContent,string correlationId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false);
            LogHandling.LogRequestDetails("CreateComponent",$"MethodName:CreateComponent(), CorrelationId: {correlationId}",httpClient,sw360ComponentApi,new StringContent(JsonConvert.SerializeObject(createComponentContent), Encoding.UTF8, ApiConstant.ApplicationJson));
            return await httpClient.PostAsJsonAsync(sw360ComponentApi, createComponentContent);
        }

        public async Task<HttpResponseMessage> CreateRelease(Releases createReleaseContent,string correlationId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false);
            LogHandling.LogRequestDetails("CreateRelease", $"MethodName:CreateRelease(), CorrelationId: {correlationId}", httpClient, sw360ReleaseApi, new StringContent(JsonConvert.SerializeObject(createReleaseContent), Encoding.UTF8, ApiConstant.ApplicationJson));
            return await httpClient.PostAsJsonAsync(sw360ReleaseApi, createReleaseContent);
        }

        public async Task<string> GetReleaseOfComponentById(string componentId, string correlationId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false);
            string componentUrl = $"{sw360ComponentApi}/{componentId}";
            LogHandling.LogRequestDetails("Get Release Of Component By Id", $"MethodName:GetReleaseOfComponentById(),CorrelationId:{correlationId}", httpClient, componentUrl);
            return await httpClient.GetStringAsync(componentUrl);
        }

        public async Task<string> GetReleaseAttachments(string releaseAttachmentsUrl)
        {
            HttpClient httpClient = GetHttpClient();
            return await httpClient.GetStringAsync(releaseAttachmentsUrl);
        }

        public async Task<string> GetAttachmentInfo(string attachmentUrl)
        {
            HttpClient httpClient = GetHttpClient();
            return await httpClient.GetStringAsync(attachmentUrl);
        }

        public void DownloadAttachmentUsingWebClient(string attachmentDownloadLink, string fileName)
        {
            var ur = new Uri(attachmentDownloadLink);
            using (WebClient client = new WebClient())
            {
                client.Headers.Add(ApiConstant.Accept, ApiConstant.ApplicationAllType);
                client.Headers.Add(ApiConstant.Authorization, $"{sw360AuthTokenType} {sw360AuthToken}");
                client.DownloadFile(ur, fileName);
            }
        }

        public async Task<HttpResponseMessage> UpdateRelease(string releaseId, HttpContent httpContent,string correlationId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false);
            string releaseApi = $"{sw360ReleaseApi}/{releaseId}";
            LogHandling.LogRequestDetails("UpdateRelease",$"MethodName:UpdateRelease(), CorrelationId: {correlationId}, ReleaseId: {releaseId}",httpClient,releaseApi,httpContent);
            return await httpClient.PatchAsync(releaseApi, httpContent);
        }

        public async Task<HttpResponseMessage> UpdateComponent(string componentId, HttpContent httpContent)
        {
            HttpClient httpClient = GetHttpClient();
            string componentApi = $"{sw360ComponentApi}/{componentId}";
            return await httpClient.PatchAsync(componentApi, httpContent);
        }


        public string AttachComponentSourceToSW360(AttachReport attachReport)
        {
            AttachmentHelper attachmentHelper = new AttachmentHelper(sw360AuthTokenType, sw360AuthToken, sw360ReleaseApi);

            return attachmentHelper.AttachComponentSourceToSW360(attachReport);
        }

        public async Task<string> GetReleaseByCompoenentName(string componentName, string correlationId)
        {
            HttpClient httpClient = GetHttpClient();
            string url = $"{sw360ReleaseNameApi}{componentName}";
            LogHandling.LogRequestDetails("Get Release By Compoenent Name", $"MethodName:GetReleaseByCompoenentName(),CorrelationId:{correlationId}", httpClient, url);
            return await httpClient.GetStringAsync(url);
        }

        public async Task<HttpResponseMessage> GetComponentDetailsByUrl(string componentLink)
        {
            HttpClient httpClient = GetHttpClient();
            return await httpClient.GetAsync(componentLink);
        }

        public async Task<string> GetComponentByName(string componentName,string correlationId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false);
            string url = $"{sw360ComponentApi}{ApiConstant.ComponentNameUrl}{componentName}";
            LogHandling.LogRequestDetails("Get Component By Name", $"MethodName:GetComponentByName(),CorrelationId:{correlationId}", httpClient, url);
            return await httpClient.GetStringAsync(url);
        }
        public async Task<HttpResponseMessage> GetComponentUsingName(string componentName)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false);
            string url = $"{sw360ComponentApi}{ApiConstant.ComponentNameUrl}{componentName}";
            return await httpClient.GetAsync(url);
        }
        public async Task<HttpResponseMessage> GetAllReleasesWithAllData(int page, int pageEntries, string correlationId)
        {
            HttpClient httpClient = GetHttpClient();
            string url = $"{sw360ReleaseApi}?page={page}&allDetails=true&page_entries={pageEntries}";
            LogHandling.LogRequestDetails("Get All Releases With All Data", $"MethodName:GetAllReleasesWithAllData(),CorrelationId:{correlationId}", httpClient, url);
            return await httpClient.GetAsync(url);
        }
        #endregion

        #region PRIVATE METHODS

        private HttpClient GetHttpClient()
        {
            var handler = new RetryHttpClientHandler()
            {
                InnerHandler = new HttpClientHandler()
            };
            var httpClient = new HttpClient(handler);
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(timeOut);
            httpClient.Timeout = timeOutInSec;
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(ApiConstant.ApplicationJson));
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(sw360AuthTokenType, sw360AuthToken);
            return httpClient;
        }

        #endregion
    }
}
