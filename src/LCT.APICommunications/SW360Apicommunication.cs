// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Interface;
using LCT.Common.Model;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    public static class HttpClientExtensions
    {
        public static void SetLogWarnings(this HttpClient client, bool logWarnings, string urlInformation)
        {
            client.DefaultRequestHeaders.Remove("LogWarnings");
            client.DefaultRequestHeaders.Remove("urlInfo");

            client.DefaultRequestHeaders.Add("LogWarnings", logWarnings.ToString());
            client.DefaultRequestHeaders.Add("urlInfo", urlInformation);
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
        private static readonly EnvironmentHelper environmentHelper = new EnvironmentHelper();
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
            httpClient.SetLogWarnings(true, "unable to get project details");
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
            httpClient.SetLogWarnings(true, "unable to get Sw360 users");
            return await httpClient.GetStringAsync(sw360UsersApi);
        }

        public async Task<string> GetProjectsByName(string projectName)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get projects by name");
            string projectNameApiUrl = $"{sw360ProjectsApi}{ApiConstant.ComponentNameUrl}{projectName}";
            return await httpClient.GetStringAsync(projectNameApiUrl);
        }

        public async Task<HttpResponseMessage> GetProjectsByTag(string projectTag)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get projects by tag");
            string projectsByTagUrl = $"{sw360ProjectByTagApi}{projectTag}";
            return await httpClient.GetAsync(projectsByTagUrl);
        }

        public async Task<HttpResponseMessage> GetProjectById(string projectId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get project details by project id");
            HttpResponseMessage obj = new HttpResponseMessage();
            var result = obj;
            string projectsByTagUrl = $"{sw360ProjectsApi}/{projectId}";
            try
            {
                result = await httpClient.GetAsync(projectsByTagUrl);
                result.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                ExceptionHandling.HttpException(ex, result, "SW360");
                environmentHelper.CallEnvironmentExit(-1);
            }
            catch (TaskCanceledException ex)
            {
                Logger.Debug($"{ex.Message}");
                ExceptionHandling.TaskCancelledException(ex, "SW360");
                environmentHelper.CallEnvironmentExit(-1);

            }
            return result;
        }

        public async Task<string> GetReleases()
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get releases");
            var result = string.Empty;
            try
            {
                HttpResponseMessage responseMessage = await httpClient.GetAsync(sw360ReleaseApi);
                if (responseMessage != null && responseMessage.StatusCode.Equals(HttpStatusCode.OK))
                {
                    return await responseMessage.Content.ReadAsStringAsync();
                }
                else
                {
                    Logger.Error("SW360 server is not accessible while getting All Releases,Please wait for sometime and re run the pipeline again." +
                        " StatusCode:" + responseMessage?.StatusCode + " & ReasonPharse :" + responseMessage?.ReasonPhrase);
                    environmentHelper.CallEnvironmentExit(-1);
                }
            }
            catch (TaskCanceledException ex)
            {
                Logger.Debug($"GetReleases():TaskCanceledException Error : {ex.Message}", ex);
                Logger.Error("TaskCanceledException error has error while getting all releases from the SW360 server,Please wait for sometime and re run the pipeline again. Error :" + ex.Message);
                environmentHelper.CallEnvironmentExit(-1);
            }
            catch (HttpRequestException ex)
            {
                Logger.Debug($"GetReleases():HttpRequestException Error : {ex.Message}", ex);
                Logger.Error("HttpRequestException error has error while getting all releases from the SW360 server,Please wait for sometime and re run the pipeline again. Error :" + ex.Message);
                environmentHelper.CallEnvironmentExit(-1);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Debug($"GetReleases():InvalidOperationException Error : {ex.Message}", ex);
                Logger.Error("InvalidOperationException error has error while getting all releases from the SW360 server,Please wait for sometime and re run the pipeline again. Error :" + ex.Message);
                environmentHelper.CallEnvironmentExit(-1);
            }
            return result;
        }
        public async Task<string> TriggerFossologyProcess(string releaseId, string sw360link)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to trigger fossology process");
            string url = $"{sw360ReleaseApi}/{releaseId}{ApiConstant.FossTriggerAPIPrefix}{sw360link}{ApiConstant.FossTriggerAPISuffix}";
            try
            {
                var response = await httpClient.GetAsync(url);
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
                Logger.Debug($"TriggerFossologyProcess(): {ex.Message}");
                throw;
            }
        }
        public async Task<HttpResponseMessage> CheckFossologyProcessStatus(string link)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to check fossology process status");
            return await httpClient.GetAsync(link);
        }
        public async Task<string> GetComponents()
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get components details");
            return await httpClient.GetStringAsync(sw360ComponentApi);
        }

        public async Task<HttpResponseMessage> GetReleaseByExternalId(string purlId, string externalIdKey = "")
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get release details by externalid");
            string releaseByExternalIdUrl = $"{sw360ReleaseByExternalId}{externalIdKey}{purlId}";
            return await httpClient.GetAsync(releaseByExternalIdUrl);
        }

        public async Task<HttpResponseMessage> GetComponentByExternalId(string purlId, string externalIdKey = "")
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get component details by externalid");
            string componentByExternalIdUrl = $"{sw360ComponentByExternalId}{externalIdKey}{purlId}";
            return await httpClient.GetAsync(componentByExternalIdUrl);
        }

        public async Task<HttpResponseMessage> GetReleaseById(string releaseId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get release details by releaseid");
            string url = $"{sw360ReleaseApi}/{releaseId}";
            return await httpClient.GetAsync(url);
        }

        public async Task<HttpResponseMessage> GetReleaseByLink(string releaseLink)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get release details by releaselink");
            return await httpClient.GetAsync(releaseLink);
        }

        public async Task<HttpResponseMessage> LinkReleasesToProject(HttpContent httpContent, string sw360ProjectId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to link releases to the project");
            string url = $"{sw360ProjectsApi}/{sw360ProjectId}/{ApiConstant.Releases}";
            return await httpClient.PostAsync(url, httpContent);
        }

        public async Task<HttpResponseMessage> UpdateLinkedRelease(string projectId, string releaseId, UpdateLinkedRelease updateLinkedRelease)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to update linked releases");
            string updateUri = $"{sw360ProjectsApi}/{projectId}/{ApiConstant.Release}/{releaseId}";
            string updateContent = JsonConvert.SerializeObject(updateLinkedRelease);
            HttpContent content = new StringContent(updateContent, Encoding.UTF8, "application/json");

            return await httpClient.PatchAsync(updateUri, content);
        }

        public async Task<HttpResponseMessage> CreateComponent(CreateComponent createComponentContent)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to create component");
            return await httpClient.PostAsJsonAsync(sw360ComponentApi, createComponentContent);
        }

        public async Task<HttpResponseMessage> CreateRelease(Releases createReleaseContent)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to create release");
            return await httpClient.PostAsJsonAsync(sw360ReleaseApi, createReleaseContent);
        }

        public async Task<string> GetReleaseOfComponentById(string componentId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get release data by component id");
            string componentUrl = $"{sw360ComponentApi}/{componentId}";
            return await httpClient.GetStringAsync(componentUrl);
        }

        public async Task<string> GetReleaseAttachments(string releaseAttachmentsUrl)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get release attachments");
            return await httpClient.GetStringAsync(releaseAttachmentsUrl);
        }

        public async Task<string> GetAttachmentInfo(string attachmentUrl)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get attachment information");
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

        public async Task<HttpResponseMessage> UpdateRelease(string releaseId, HttpContent httpContent)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to update the release data");
            string releaseApi = $"{sw360ReleaseApi}/{releaseId}";
            return await httpClient.PatchAsync(releaseApi, httpContent);
        }

        public async Task<HttpResponseMessage> UpdateComponent(string componentId, HttpContent httpContent)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to update the component data");
            string componentApi = $"{sw360ComponentApi}/{componentId}";
            return await httpClient.PatchAsync(componentApi, httpContent);
        }


        public string AttachComponentSourceToSW360(AttachReport attachReport,ComparisonBomData comparisonBomData)
        {
            AttachmentHelper attachmentHelper = new AttachmentHelper(sw360AuthTokenType, sw360AuthToken, sw360ReleaseApi);

            return attachmentHelper.AttachComponentSourceToSW360(attachReport,comparisonBomData);
        }

        public async Task<string> GetReleaseByCompoenentName(string componentName)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get release data by component name");
            string url = $"{sw360ReleaseNameApi}{componentName}";
            return await httpClient.GetStringAsync(url);
        }

        public async Task<HttpResponseMessage> GetComponentDetailsByUrl(string componentLink)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get component details by component link");
            return await httpClient.GetAsync(componentLink);
        }

        public async Task<string> GetComponentByName(string componentName)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get component details by component name");
            string url = $"{sw360ComponentApi}{ApiConstant.ComponentNameUrl}{componentName}";
            return await httpClient.GetStringAsync(url);
        }
        public async Task<HttpResponseMessage> GetComponentUsingName(string componentName)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get component details by component name");
            string url = $"{sw360ComponentApi}{ApiConstant.ComponentNameUrl}{componentName}";
            return await httpClient.GetAsync(url);
        }
        public async Task<HttpResponseMessage> GetAllReleasesWithAllData(int page, int pageEntries)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get all releases details");
            string url = $"{sw360ReleaseApi}?page={page}&allDetails=true&page_entries={pageEntries}";
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
