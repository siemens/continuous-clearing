// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using log4net;
using Microsoft.Web.Administration;
using Newtonsoft.Json;
using SIT.APICommunications;
using SIT.APICommunications.Model;
using SIT.APICommunications.Model.Foss;
using SIT.Common;
using SIT.Common.Constants;
using SIT.Common.Interface;
using SIT.Create.Model;
using SIT.Facade.Interfaces;
using SIT.Services;
using SIT.Services.Interface;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;


namespace SIT.Create
{
    /// <summary>
    /// Validates the creator param
    /// </summary>
    public static class CreatorValidator
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string FossologyUrlValidationContext = "Fossology URL Validation";

        public static async Task<int> ValidateAppSettings(CommonAppSettings appSettings, ISw360ProjectService sw360ProjectService, ProjectReleases projectReleases)
        {
            string sw360ProjectName = await sw360ProjectService.GetProjectNameByProjectIDFromSW360(appSettings.SW360.ProjectID, appSettings.SW360.ProjectName, projectReleases);

            return CommonHelper.ValidateSw360Project(sw360ProjectName, projectReleases?.ClearingState, projectReleases?.Name, appSettings);
        }
        public static async Task TriggerFossologyValidation(CommonAppSettings appSettings, ISW360ApicommunicationFacade sW360ApicommunicationFacade, IEnvironmentHelper environmentHelper)
        {
            Logger.Debug("TriggerFossologyValidation(): Starting trigger fossology validation process.");
            ISW360CommonService sw360CommonService = new SW360CommonService(sW360ApicommunicationFacade);
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sW360ApicommunicationFacade, sw360CommonService);

            try
            {
                ReleasesAllDetails.Sw360Release validRelease = await FindValidRelease(sW360ApicommunicationFacade);

                if (validRelease != null)
                {
                    Logger.DebugFormat("TriggerFossologyValidation(): Valid release found. Identified component Name-{0},Version-{1}.", validRelease.Name, validRelease.Version);
                    await TriggerFossologyProcessForRelease(validRelease, appSettings, sw360CreatorService);
                }
                else
                {
                    Logger.Debug($"TriggerFossologyValidation(): No valid release found. Fossology URL validation failed");
                    Logger.Error("Fossology URL validation failed due to valid release not found from SW360");
                    environmentHelper.CallEnvironmentExit(-1);
                }
                Logger.Debug("TriggerFossologyValidation(): Completed trigger fossology validation process.");
            }
            catch (AggregateException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Fossology Validation", $"MethodName:TriggerFossologyValidation()", ex, "");
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"TriggerFossologyValidation(): {ex.Message}", ex);
                LogHandlingHelper.ExceptionErrorHandling("Fossology Validation", "TriggerFossologyValidation()", ex, "Investigate the exception details.");
            }
        }

        /// <summary>
        /// Finds Valid Release
        /// </summary>
        /// <param name="sW360ApicommunicationFacade"></param>
        /// <returns></returns>
        private static async Task<ReleasesAllDetails.Sw360Release> FindValidRelease(ISW360ApicommunicationFacade sW360ApicommunicationFacade)
        {
            ReleasesAllDetails firstPageResponse = await GetAllReleasesDetails(sW360ApicommunicationFacade, 0, ApiConstant.ReleaseListPageSize);
            if (firstPageResponse == null)
            {
                Logger.Debug($"FindValidRelease(): Fossology token validation failed in SW360 due to release not found");
                return null;
            }

            var validRelease = TryFindValidReleaseInPage(firstPageResponse);
            if (validRelease != null)
            {
                return validRelease;
            }

            // Worst-case datasets span hundreds of pages, and a match could be on the very last one, so scanning
            // every page sequentially isn't safe to cap. Fetch remaining pages in concurrent batches instead,
            // stopping as soon as any page in a batch yields a match.
            int totalPages = firstPageResponse.Page?.TotalPages ?? 1;
            foreach (int[] batch in Enumerable.Range(1, Math.Max(0, totalPages - 1)).Chunk(ApiConstant.MaxParallelPageRequests))
            {
                ReleasesAllDetails[] pageResponses = await Task.WhenAll(batch.Select(page =>
                    GetAllReleasesDetails(sW360ApicommunicationFacade, page, ApiConstant.ReleaseListPageSize)));

                foreach (ReleasesAllDetails pageResponse in pageResponses)
                {
                    Logger.Debug($"FindValidRelease(): Release response data: {JsonConvert.SerializeObject(pageResponse)}");
                    validRelease = TryFindValidReleaseInPage(pageResponse);
                    if (validRelease != null)
                    {
                        return validRelease;
                    }
                }
            }

            Logger.Debug($"FindValidRelease(): No valid release found across all pages");
            return null;
        }

        /// <summary>
        /// Finds the first release in a page that is already APPROVED and has a SOURCE attachment, making it a
        /// safe target to probe the Fossology connection.
        /// </summary>
        /// <param name="releaseResponse"></param>
        /// <returns>the matching release, or null if the page has no qualifying release</returns>
        private static ReleasesAllDetails.Sw360Release TryFindValidReleaseInPage(ReleasesAllDetails releaseResponse)
        {
            const string source = "SOURCE";

            return releaseResponse?.Embedded?.Sw360releases?.FirstOrDefault(release =>
                release?.ClearingState == Dataconstant.Approved &&
                release.AllReleasesEmbedded?.Sw360attachments != null &&
                release.AllReleasesEmbedded.Sw360attachments.Any(attachment => source.Equals(attachment?.AttachmentType, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Triggers Fossology Process For Release
        /// </summary>
        /// <param name="validRelease"></param>
        /// <param name="appSettings"></param>
        /// <param name="sw360CreatorService"></param>
        /// <returns>task that returns asynchronous operation</returns>
        private static async Task TriggerFossologyProcessForRelease(ReleasesAllDetails.Sw360Release validRelease, CommonAppSettings appSettings, ISw360CreatorService sw360CreatorService)
        {
            var releaseUrl = validRelease?.Links?.Self?.Href;
            var releaseId = releaseUrl != null ? CommonHelper.GetSubstringOfLastOccurance(releaseUrl, "/") : string.Empty;

            string sw360link = $"{validRelease?.Name}:{validRelease?.Version}:{appSettings?.SW360?.URL}{ApiConstant.Sw360ReleaseUrlApiSuffix}" +
                               $"{releaseId}#/tab-Summary";

            FossTriggerStatus fossResult = await sw360CreatorService.TriggerFossologyProcessForValidation(releaseId, sw360link);

            if (!string.IsNullOrEmpty(fossResult?.Links?.Self?.Href))
            {
                Logger.Debug($"TriggerFossologyValidation(): SW360 Fossology Process validation successful!!");
            }
        }

        /// <summary>
        /// Gets All Releases Details
        /// </summary>
        /// <param name="sW360ApicommunicationFacade"></param>
        /// <param name="page"></param>
        /// <param name="pageEntries"></param>
        /// <returns>release details</returns>
        private static async Task<ReleasesAllDetails> GetAllReleasesDetails(ISW360ApicommunicationFacade sW360ApicommunicationFacade, int page, int pageEntries)
        {
            ReleasesAllDetails releaseResponse = null;
            try
            {
                var responseData = await sW360ApicommunicationFacade.GetAllReleasesWithAllData(page, pageEntries);
                await LogHandlingHelper.HttpResponseHandling("Get All Releases Details", $"MethodName:GetAllReleasesDetails()", responseData);
                string response = responseData?.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                releaseResponse = JsonConvert.DeserializeObject<ReleasesAllDetails>(response);
            }
            catch (HttpRequestException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("HttpRequestException while Get All Releases Details", $"MethodName:GetAllReleasesDetails()", ex, "Investigate the HttpRequestException details to identify the root cause.");
            }
            catch (InvalidOperationException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("InvalidOperationException while Get All Releases Details", $"MethodName:GetAllReleasesDetails()", ex, "Investigate the InvalidOperationException details to identify the root cause.");
            }
            catch (UriFormatException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("UriFormatException while Get All Releases Details", $"MethodName:GetAllReleasesDetails()", ex, "Investigate the UriFormatException details to identify the root cause.");
            }
            catch (TaskCanceledException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("TaskCanceledException while Get All Releases Details", $"MethodName:GetAllReleasesDetails()", ex, "Investigate the TaskCanceledException details to identify the root cause.");
            }

            return releaseResponse;
        }
        /// <summary>
        /// Fossology Url Validation
        /// </summary>
        /// <param name="appSettings"></param>
        /// <param name="client"></param>
        /// <param name="environmentHelper"></param>
        /// <returns>task that represents asynchronous operation</returns>
        public static async Task<bool> FossologyUrlValidation(CommonAppSettings appSettings, HttpClient client, IEnvironmentHelper environmentHelper)
        {
            Logger.Debug("FossologyUrlValidation(): Starting Fossology URL validation process.");
            string url = appSettings.SW360.Fossology.URL;
            if (string.IsNullOrEmpty(url))
            {
                Logger.Error($"Fossology URL is not provided. Please make sure to add Fossology URL in appsettings.");
                LogHandlingHelper.BasicErrorHandling(FossologyUrlValidationContext, "FossologyUrlValidation", "Fossology URL is not provided. Please ensure the Fossology URL is configured in appsettings.", "Add a valid Fossology URL in the appsettings configuration.");
                environmentHelper.CallEnvironmentExit(-1);
                return false;
            }
            url = url.ToLower();
            string prodFossUrl = Dataconstant.ProductionFossologyURL.ToLower();
            string stageFossUrl = Dataconstant.StageFossologyURL.ToLower();

            if (Uri.IsWellFormedUriString(appSettings.SW360.Fossology.URL, UriKind.Absolute))
            {
                if (url.Contains(prodFossUrl) || url.Contains(stageFossUrl))
                {
                    // Send GET request to validate Fossology URL
                    try
                    {
                        await LogHandlingHelper.HttpRequestHandling(FossologyUrlValidationContext, $"Methodname:FossologyUrlValidation()", client, url);
                        HttpResponseMessage response = await client.GetAsync(new Uri(appSettings.SW360.Fossology.URL));
                        await LogHandlingHelper.HttpResponseHandling(FossologyUrlValidationContext, $"Methodname:FossologyUrlValidation()", response);
                        if (response.IsSuccessStatusCode)
                        {
                            // Fossology URL is valid                            
                            Logger.Debug("FossologyUrlValidation(): Completed Fossology URL validation process.");
                            return true;
                        }
                        else
                        {
                            // Fossology URL is not valid                                   
                            Logger.Error($"Fossology URL is not valid. Please make sure to add a valid Fossology URL in appsettings.");
                            LogHandlingHelper.ExceptionErrorHandling(FossologyUrlValidationContext, $"Methodname:FossologyUrlValidation()", new Exception($"Fossology URL not working. Received HTTP status code: {response.StatusCode}. URL: {url}"), $"Ensure the Fossology URL is accessible and returns a successful response. URL: {url}");
                            environmentHelper.CallEnvironmentExit(-1);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        // Fossology URL is not valid                                   
                        Logger.Error($"Fossology URL is not working. Please check and try again.", ex);
                        LogHandlingHelper.ExceptionErrorHandling("HttpRequestException while Fossology URL Validation", $"Methodname:FossologyUrlValidation()", ex, "Check the network connection and ensure the Fossology server is reachable.");
                        environmentHelper.CallEnvironmentExit(-1);
                    }
                    catch (TaskCanceledException ex)
                    {
                        // Request timed out (HttpClient.Timeout elapsed) rather than failing outright
                        Logger.Error($"Fossology URL validation timed out. Please check and try again.", ex);
                        LogHandlingHelper.ExceptionErrorHandling("TaskCanceledException while Fossology URL Validation", $"Methodname:FossologyUrlValidation()", ex, "The Fossology server took too long to respond. Check connectivity or increase the timeout and try again.");
                        environmentHelper.CallEnvironmentExit(-1);
                    }
                }
                else
                {
                    Logger.Debug($"FossologyUrlValidation(): Fossology URL is not valid.");
                    LogHandlingHelper.BasicErrorHandling(FossologyUrlValidationContext, $"Methodname:FossologyUrlValidation()", $"Fossology URL does not match the configured production or staging URLs. URL: {url}", "Ensure the Fossology URL matches the configured production or staging URLs.");
                    environmentHelper.CallEnvironmentExit(-1);
                }
            }
            else
            {
                Logger.Error($"Fossology URL is not valid. Please make sure to add a valid Fossology URL in appsettings.");
                LogHandlingHelper.BasicErrorHandling(FossologyUrlValidationContext, $"Methodname:FossologyUrlValidation()", "The provided Fossology URL is not a valid absolute URI.", "Check the Fossology URL format in the appsettings configuration.");
                environmentHelper.CallEnvironmentExit(-1);
            }
            Logger.Debug("FossologyUrlValidation(): Completed Fossology URL validation process with failure.");
            return false;
        }

    }
}
