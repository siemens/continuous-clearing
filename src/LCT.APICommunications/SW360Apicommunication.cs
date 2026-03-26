// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.Common;
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
    /// <summary>
    /// Provides extension methods for HttpClient.
    /// </summary>
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Sets log warning headers on the HttpClient for diagnostic purposes.
        /// </summary>
        /// <param name="client">The HttpClient instance to configure.</param>
        /// <param name="logWarnings">Indicates whether warnings should be logged.</param>
        /// <param name="urlInformation">Additional URL information for logging context.</param>
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
    public class SW360Apicommunication(SW360ConnectionSettings sw360ConnectionSettings) : ISw360ApiCommunication
    {
        #region Fields
        private readonly string sw360AuthTokenType = sw360ConnectionSettings.SW360AuthTokenType;
        private readonly string sw360AuthToken = sw360ConnectionSettings.Sw360Token;
        private readonly string sw360ComponentApi = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360ComponentApiSuffix}";
        private readonly string sw360ReleaseApi = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360ReleaseApiSuffix}";
        private readonly string sw360ProjectsApi = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360ProjectsApiSuffix}";
        private readonly string sw360ReleaseNameApi = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360ReleaseNameApiSuffix}";
        private readonly string sw360ProjectByTagApi = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360ProjectByTagApiSuffix}";
        private readonly string sw360ReleaseByExternalId = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360ReleaseByExternalId}";
        private readonly string sw360ComponentByExternalId = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360ComponentByExternalId}";
        private readonly string sw360UsersApi = $"{sw360ConnectionSettings.SW360URL}{ApiConstant.Sw360UsersSuffix}";
        private readonly int timeOut = sw360ConnectionSettings.Timeout;
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly EnvironmentHelper environmentHelper = new EnvironmentHelper();
        private const string GetReleasesMessage = "Get Releases";
        private const string TriggerFossologyMessage = "Trigger Fossology Process";
        #endregion

        #region Methods

        /// <summary>
        /// Asynchronously retrieves all projects from SW360.
        /// </summary>
        /// <returns>A JSON string containing the project data.</returns>
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
                LogHandlingHelper.ExceptionErrorHandling("Get sw360 Projects details", $"MethodName:GetProjects(),A timeout error is thrown from SW360 server,Please wait for sometime and re run the pipeline again", ex, "");
                Logger.ErrorFormat("A timeout error is thrown from SW360 server,Please wait for sometime and re run the pipeline again. {0}", ex.Message);
                environmentHelper.CallEnvironmentExit(-1);
            }
            return result;
        }

        /// <summary>
        /// Asynchronously retrieves all SW360 users.
        /// </summary>
        /// <returns>A JSON string containing the SW360 users data.</returns>
        public async Task<string> GetSw360Users()
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get Sw360 users");
            return await httpClient.GetStringAsync(sw360UsersApi);
        }

        /// <summary>
        /// Asynchronously retrieves projects by their name from SW360.
        /// </summary>
        /// <param name="projectName">The name of the project to search for.</param>
        /// <returns>A JSON string containing the matching project data.</returns>
        public async Task<string> GetProjectsByName(string projectName)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get projects by name");
            string projectNameApiUrl = $"{sw360ProjectsApi}{ApiConstant.ComponentNameUrl}{projectName}";
            return await httpClient.GetStringAsync(projectNameApiUrl);
        }

        /// <summary>
        /// Asynchronously retrieves projects by their tag from SW360.
        /// </summary>
        /// <param name="projectTag">The tag to filter projects by.</param>
        /// <returns>An HttpResponseMessage containing the matching project data.</returns>
        public async Task<HttpResponseMessage> GetProjectsByTag(string projectTag)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get projects by tag");
            string projectsByTagUrl = $"{sw360ProjectByTagApi}{projectTag}";
            return await httpClient.GetAsync(projectsByTagUrl);
        }

        /// <summary>
        /// Asynchronously retrieves a project by its unique identifier from SW360.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <returns>An HttpResponseMessage containing the project data.</returns>
        public async Task<HttpResponseMessage> GetProjectById(string projectId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get project details by project id");
            HttpResponseMessage obj = new HttpResponseMessage();
            var result = obj;
            string projectsByTagUrl = $"{sw360ProjectsApi}/{projectId}";
            await LogHandlingHelper.HttpRequestHandling("Get sw360 Project details for validating", $"MethodName:GetProjectById()", httpClient, projectsByTagUrl);
            try
            {
                result = await httpClient.GetAsync(projectsByTagUrl);
                await LogHandlingHelper.HttpResponseHandling("Get sw360 Project details", $"MethodName:GetProjectById()", result);
                result.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get sw360 Project details for validating", $"MethodName:GetProjectById()", ex, "");
                ExceptionHandling.HttpException(ex, result, "SW360");
                environmentHelper.CallEnvironmentExit(-1);
            }
            catch (TaskCanceledException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get sw360 Project details for validating", $"MethodName:GetProjectById()", ex, "");
                ExceptionHandling.TaskCancelledException(ex, "SW360");
                environmentHelper.CallEnvironmentExit(-1);

            }
            return result;
        }

        /// <summary>
        /// Asynchronously retrieves all releases from SW360.
        /// </summary>
        /// <returns>A JSON string containing the release data.</returns>
        public async Task<string> GetReleases()
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "Unable to retrieve details for SW360 releases.");
            var result = string.Empty;
            try
            {
                await LogHandlingHelper.HttpRequestHandling("Request for get all releases", $"MethodName:GetReleases()", httpClient, sw360ReleaseApi);
                HttpResponseMessage responseMessage = await httpClient.GetAsync(sw360ReleaseApi);
                await LogHandlingHelper.HttpResponseHandling("Response of get all releases", $"MethodName:GetReleases()", responseMessage);
                if (responseMessage != null && responseMessage.StatusCode.Equals(HttpStatusCode.OK))
                {
                    return await responseMessage.Content.ReadAsStringAsync();
                }
                else
                {
                    LogHandlingHelper.BasicErrorHandling(GetReleasesMessage, $"MethodName:GetReleases()",
                $"SW360 server is not accessible. StatusCode: {responseMessage?.StatusCode}, ReasonPhrase: {responseMessage?.ReasonPhrase}",
                "Please wait for some time and re-run the pipeline.");
                    Logger.ErrorFormat("SW360 server is not accessible while getting All Releases,Please wait for sometime and re run the pipeline again..." +
                        "StatusCode:{0} & ReasonPhrase:{1}", responseMessage?.StatusCode, responseMessage?.ReasonPhrase);
                    environmentHelper.CallEnvironmentExit(-1);
                }
            }
            catch (TaskCanceledException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling(GetReleasesMessage, $"MethodName:GetReleases()", ex,
            "TaskCanceledException occurred while getting all releases from the SW360 server. Please wait for some time and re-run the pipeline.");
                Logger.ErrorFormat("TaskCanceledException error has error while getting all releases from the SW360 server,Please wait for sometime and re run the pipeline again. Error : {0}",  ex.Message);
                environmentHelper.CallEnvironmentExit(-1);
            }
            catch (HttpRequestException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling(GetReleasesMessage, $"MethodName:GetReleases()", ex, "");
                Logger.ErrorFormat("HttpRequestException error has error while getting all releases from the SW360 server,Please wait for sometime and re run the pipeline again. Error : {0}" , ex.Message);
                environmentHelper.CallEnvironmentExit(-1);
            }
            catch (InvalidOperationException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling(GetReleasesMessage, $"MethodName:GetReleases()", ex,
            "InvalidOperationException occurred while getting all releases from the SW360 server. Please wait for some time and re-run the pipeline.");
                Logger.ErrorFormat("InvalidOperationException error has error while getting all releases from the SW360 server,Please wait for sometime and re run the pipeline again. Error : {0}", ex.Message);
                environmentHelper.CallEnvironmentExit(-1);
            }
            return result;
        }

        /// <summary>
        /// Asynchronously triggers the Fossology scanning process for a release.
        /// </summary>
        /// <param name="releaseId">The unique identifier of the release.</param>
        /// <param name="sw360link">The SW360 link for the Fossology process.</param>
        /// <returns>A JSON string containing the Fossology process response.</returns>
        public async Task<string> TriggerFossologyProcess(string releaseId, string sw360link)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to trigger fossology process");
            string url = $"{sw360ReleaseApi}/{releaseId}{ApiConstant.FossTriggerAPIPrefix}{sw360link}{ApiConstant.FossTriggerAPISuffix}";
            try
            {
                await LogHandlingHelper.HttpRequestHandling(TriggerFossologyMessage, $"MethodName:TriggerFossologyProcess()", httpClient, url);
                var response = await httpClient.GetAsync(url);
                await LogHandlingHelper.HttpResponseHandling(TriggerFossologyMessage, $"MethodName:TriggerFossologyProcess()", response);
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
                LogHandlingHelper.ExceptionErrorHandling(TriggerFossologyMessage, $"MethodName:TriggerFossologyProcess()", ex, "An HTTP request error occurred while triggering the Fossology process.");
                throw;
            }
        }

        /// <summary>
        /// Asynchronously checks the status of a Fossology scanning process.
        /// </summary>
        /// <param name="link">The link to check the Fossology process status.</param>
        /// <returns>An HttpResponseMessage containing the process status.</returns>
        public async Task<HttpResponseMessage> CheckFossologyProcessStatus(string link)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to check fossology process status");
            await LogHandlingHelper.HttpRequestHandling(TriggerFossologyMessage, $"MethodName:TriggerFossologyProcess()", httpClient, link);
            return await httpClient.GetAsync(link);
        }

        /// <summary>
        /// Asynchronously retrieves all components from SW360.
        /// </summary>
        /// <returns>A JSON string containing the component data.</returns>
        public async Task<string> GetComponents()
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get components details");
            await LogHandlingHelper.HttpRequestHandling("Request for get components data", $"MethodName:GetComponents()", httpClient, sw360ComponentApi);
            return await httpClient.GetStringAsync(sw360ComponentApi);
        }

        /// <summary>
        /// Asynchronously retrieves a release by its external identifier.
        /// </summary>
        /// <param name="purlId">The Package URL identifier.</param>
        /// <param name="externalIdKey">The external identifier key prefix.</param>
        /// <returns>An HttpResponseMessage containing the release data.</returns>
        public async Task<HttpResponseMessage> GetReleaseByExternalId(string purlId, string externalIdKey = "")
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get release details by externalid");
            string releaseByExternalIdUrl = $"{sw360ReleaseByExternalId}{externalIdKey}{purlId}";
            await LogHandlingHelper.HttpRequestHandling("Request for get release data by ExternalId", $"MethodName:GetReleaseByExternalId()", httpClient, releaseByExternalIdUrl);
            return await httpClient.GetAsync(releaseByExternalIdUrl);
        }

        /// <summary>
        /// Asynchronously retrieves a component by its external identifier.
        /// </summary>
        /// <param name="purlId">The Package URL identifier.</param>
        /// <param name="externalIdKey">The external identifier key prefix.</param>
        /// <returns>An HttpResponseMessage containing the component data.</returns>
        public async Task<HttpResponseMessage> GetComponentByExternalId(string purlId, string externalIdKey = "")
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get component details by externalid");
            string componentByExternalIdUrl = $"{sw360ComponentByExternalId}{externalIdKey}{purlId}";
            await LogHandlingHelper.HttpRequestHandling("Request for get component data by ExternalId", $"MethodName:GetComponentByExternalId()", httpClient, componentByExternalIdUrl);
            return await httpClient.GetAsync(componentByExternalIdUrl);
        }

        /// <summary>
        /// Asynchronously retrieves a release by its unique identifier.
        /// </summary>
        /// <param name="releaseId">The unique identifier of the release.</param>
        /// <returns>An HttpResponseMessage containing the release data.</returns>
        public async Task<HttpResponseMessage> GetReleaseById(string releaseId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get release details by releaseid");
            string url = $"{sw360ReleaseApi}/{releaseId}";
            await LogHandlingHelper.HttpRequestHandling("Request for get release data by ReleaseId", $"MethodName:GetReleaseById()", httpClient, url);
            return await httpClient.GetAsync(url);
        }

        /// <summary>
        /// Asynchronously retrieves a release by its link URL.
        /// </summary>
        /// <param name="releaseLink">The link URL of the release.</param>
        /// <returns>An HttpResponseMessage containing the release data.</returns>
        public async Task<HttpResponseMessage> GetReleaseByLink(string releaseLink)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get release details by releaselink");
            return await httpClient.GetAsync(releaseLink);
        }

        /// <summary>
        /// Asynchronously links releases to a specified SW360 project.
        /// </summary>
        /// <param name="httpContent">The HTTP content containing the release link data.</param>
        /// <param name="sw360ProjectId">The unique identifier of the SW360 project.</param>
        /// <returns>An HttpResponseMessage indicating the result of the link operation.</returns>
        public async Task<HttpResponseMessage> LinkReleasesToProject(HttpContent httpContent, string sw360ProjectId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to link releases to the project");
            string url = $"{sw360ProjectsApi}/{sw360ProjectId}/{ApiConstant.Releases}";
            await LogHandlingHelper.HttpRequestHandling("LinkReleasesToProject", $"MethodName:LinkReleasesToProject(), ProjectId: {sw360ProjectId}", httpClient, url, httpContent);
            return await httpClient.PostAsync(url, httpContent);
        }

        /// <summary>
        /// Asynchronously updates a linked release within a project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="releaseId">The unique identifier of the release to update.</param>
        /// <param name="updateLinkedRelease">The update data for the linked release.</param>
        /// <returns>An HttpResponseMessage indicating the result of the update operation.</returns>
        public async Task<HttpResponseMessage> UpdateLinkedRelease(string projectId, string releaseId, UpdateLinkedRelease updateLinkedRelease)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to update linked releases");
            string updateUri = $"{sw360ProjectsApi}/{projectId}/{ApiConstant.Release}/{releaseId}";
            string updateContent = JsonConvert.SerializeObject(updateLinkedRelease);
            HttpContent content = new StringContent(updateContent, Encoding.UTF8, "application/json");

            return await httpClient.PatchAsync(updateUri, content);
        }

        /// <summary>
        /// Asynchronously creates a new component in SW360.
        /// </summary>
        /// <param name="createComponentContent">The component data to create.</param>
        /// <returns>An HttpResponseMessage indicating the result of the create operation.</returns>
        public async Task<HttpResponseMessage> CreateComponent(CreateComponent createComponentContent)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to create component");
            await LogHandlingHelper.HttpRequestHandling("CreateComponent", $"MethodName:CreateComponent()", httpClient, sw360ComponentApi, new StringContent(JsonConvert.SerializeObject(createComponentContent), Encoding.UTF8, ApiConstant.ApplicationJson));
            return await httpClient.PostAsJsonAsync(sw360ComponentApi, createComponentContent);
        }

        /// <summary>
        /// Asynchronously creates a new release in SW360.
        /// </summary>
        /// <param name="createReleaseContent">The release data to create.</param>
        /// <returns>An HttpResponseMessage indicating the result of the create operation.</returns>
        public async Task<HttpResponseMessage> CreateRelease(Releases createReleaseContent)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to create release");
            await LogHandlingHelper.HttpRequestHandling("CreateRelease", $"MethodName:CreateRelease()", httpClient, sw360ReleaseApi, new StringContent(JsonConvert.SerializeObject(createReleaseContent), Encoding.UTF8, ApiConstant.ApplicationJson));
            return await httpClient.PostAsJsonAsync(sw360ReleaseApi, createReleaseContent);
        }

        /// <summary>
        /// Asynchronously retrieves release information for a component by its identifier.
        /// </summary>
        /// <param name="componentId">The unique identifier of the component.</param>
        /// <returns>A JSON string containing the release data for the component.</returns>
        public async Task<string> GetReleaseOfComponentById(string componentId)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get release data by component id");
            string componentUrl = $"{sw360ComponentApi}/{componentId}";
            await LogHandlingHelper.HttpRequestHandling("Get Release Of Component By Id", $"MethodName:GetReleaseOfComponentById()", httpClient, componentUrl);
            return await httpClient.GetStringAsync(componentUrl);
        }

        /// <summary>
        /// Asynchronously retrieves attachments for a release.
        /// </summary>
        /// <param name="releaseAttachmentsUrl">The URL to retrieve release attachments from.</param>
        /// <returns>A JSON string containing the release attachments data.</returns>
        public async Task<string> GetReleaseAttachments(string releaseAttachmentsUrl)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get release attachments");
            return await httpClient.GetStringAsync(releaseAttachmentsUrl);
        }

        /// <summary>
        /// Asynchronously retrieves information about a specific attachment.
        /// </summary>
        /// <param name="attachmentUrl">The URL of the attachment to retrieve information for.</param>
        /// <returns>A JSON string containing the attachment information.</returns>
        public async Task<string> GetAttachmentInfo(string attachmentUrl)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get attachment information");
            return await httpClient.GetStringAsync(attachmentUrl);
        }

        /// <summary>
        /// Downloads an attachment using WebClient to a specified file.
        /// </summary>
        /// <param name="attachmentDownloadLink">The download link for the attachment.</param>
        /// <param name="fileName">The destination file name to save the attachment.</param>
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

        /// <summary>
        /// Asynchronously updates an existing release in SW360.
        /// </summary>
        /// <param name="releaseId">The unique identifier of the release to update.</param>
        /// <param name="httpContent">The HTTP content containing the update data.</param>
        /// <returns>An HttpResponseMessage indicating the result of the update operation.</returns>
        public async Task<HttpResponseMessage> UpdateRelease(string releaseId, HttpContent httpContent)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to update the release data");
            string releaseApi = $"{sw360ReleaseApi}/{releaseId}";
            await LogHandlingHelper.HttpRequestHandling("UpdateRelease", $"MethodName:UpdateRelease(), ReleaseId: {releaseId}", httpClient, releaseApi, httpContent);
            return await httpClient.PatchAsync(releaseApi, httpContent);
        }

        /// <summary>
        /// Asynchronously updates an existing component in SW360.
        /// </summary>
        /// <param name="componentId">The unique identifier of the component to update.</param>
        /// <param name="httpContent">The HTTP content containing the update data.</param>
        /// <returns>An HttpResponseMessage indicating the result of the update operation.</returns>
        public async Task<HttpResponseMessage> UpdateComponent(string componentId, HttpContent httpContent)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to update the component data");
            string componentApi = $"{sw360ComponentApi}/{componentId}";
            return await httpClient.PatchAsync(componentApi, httpContent);
        }

        /// <summary>
        /// Attaches component source to SW360 for a given report and comparison data.
        /// </summary>
        /// <param name="attachReport">The attachment report containing source details.</param>
        /// <param name="comparisonBomData">The comparison BOM data for the component.</param>
        /// <returns>A string indicating the result of the attachment operation.</returns>
        public string AttachComponentSourceToSW360(AttachReport attachReport, ComparisonBomData comparisonBomData)
        {
            AttachmentHelper attachmentHelper = new AttachmentHelper(sw360AuthTokenType, sw360AuthToken, sw360ReleaseApi);

            return attachmentHelper.AttachComponentSourceToSW360(attachReport, comparisonBomData);
        }

        /// <summary>
        /// Asynchronously retrieves releases by component name.
        /// </summary>
        /// <param name="componentName">The name of the component to search releases for.</param>
        /// <returns>A JSON string containing the release data.</returns>
        public async Task<string> GetReleaseByCompoenentName(string componentName)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get release data by component name");
            string url = $"{sw360ReleaseNameApi}{componentName}";
            await LogHandlingHelper.HttpRequestHandling("Get Release By Compoenent Name", $"MethodName:GetReleaseByCompoenentName()", httpClient, url);
            return await httpClient.GetStringAsync(url);
        }

        /// <summary>
        /// Asynchronously retrieves component details by its URL.
        /// </summary>
        /// <param name="componentLink">The URL link to the component.</param>
        /// <returns>An HttpResponseMessage containing the component details.</returns>
        public async Task<HttpResponseMessage> GetComponentDetailsByUrl(string componentLink)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "unable to get component details by component link");
            return await httpClient.GetAsync(componentLink);
        }

        /// <summary>
        /// Asynchronously retrieves a component by its name.
        /// </summary>
        /// <param name="componentName">The name of the component to retrieve.</param>
        /// <returns>A JSON string containing the component data.</returns>
        public async Task<string> GetComponentByName(string componentName)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get component details by component name");
            string url = $"{sw360ComponentApi}{ApiConstant.ComponentNameUrl}{componentName}";
            await LogHandlingHelper.HttpRequestHandling("Get Component By Name", $"MethodName:GetComponentByName()", httpClient, url);
            return await httpClient.GetStringAsync(url);
        }

        /// <summary>
        /// Asynchronously retrieves a component using its name and returns the full HTTP response.
        /// </summary>
        /// <param name="componentName">The name of the component to retrieve.</param>
        /// <returns>An HttpResponseMessage containing the component data.</returns>
        public async Task<HttpResponseMessage> GetComponentUsingName(string componentName)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(false, "unable to get component details by component name");
            string url = $"{sw360ComponentApi}{ApiConstant.ComponentNameUrl}{componentName}";
            return await httpClient.GetAsync(url);
        }

        /// <summary>
        /// Asynchronously retrieves all releases with complete data using pagination.
        /// </summary>
        /// <param name="page">The page number to retrieve.</param>
        /// <param name="pageEntries">The number of entries per page.</param>
        /// <returns>An HttpResponseMessage containing the paginated release data with all details.</returns>
        public async Task<HttpResponseMessage> GetAllReleasesWithAllData(int page, int pageEntries)
        {
            HttpClient httpClient = GetHttpClient();
            httpClient.SetLogWarnings(true, "Unable to retrieve full details for SW360 releases.");
            string url = $"{sw360ReleaseApi}?page={page}&allDetails=true&page_entries={pageEntries}";
            await LogHandlingHelper.HttpRequestHandling("Get All Releases With All Data", $"MethodName:GetAllReleasesWithAllData()", httpClient, url);
            return await httpClient.GetAsync(url);
        }

        /// <summary>
        /// Creates and configures an HttpClient instance with authentication and timeout settings.
        /// </summary>
        /// <returns>A configured HttpClient instance for SW360 API communication.</returns>
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

        #endregion Methods
    }
}
