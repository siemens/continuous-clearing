// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.Foss;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Logging;
using LCT.Facade.Interfaces;
using LCT.Services;
using LCT.Services.Interface;
using LCT.SW360PackageCreator.Model;
using log4net;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;


namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// Validates the creator param
    /// </summary>
    public static class CreatorValidator
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static IEnvironmentHelper environmentHelper = new EnvironmentHelper();
        public static async Task<int> ValidateAppSettings(CommonAppSettings appSettings, ISw360ProjectService sw360ProjectService, ProjectReleases projectReleases)
        {
            Logger.Debug("ValidateAppSettings():Validation of SW360 details has started.");
            string sw360ProjectName = await sw360ProjectService.GetProjectNameByProjectIDFromSW360(appSettings.SW360.ProjectID, appSettings.SW360.ProjectName, projectReleases);

            if (string.IsNullOrEmpty(sw360ProjectName))
            {
                throw new InvalidDataException($"Invalid Project Id - {appSettings.SW360.ProjectID}");
            }
            else if (projectReleases?.clearingState == "CLOSED")
            {
                Logger.Error($"Provided Sw360 project is not in active state ,Please make sure you added the correct project details that is in active state..");
                LogHandling.BasicErrorHandelingForLog("Validation failed: SW360 project is not in an active state", "ValidateAppSettings()", $"SW360 project '{projectReleases.Name}' is in a '{projectReleases.clearingState}' state.", "Please make sure you added the correct project details that is in active state..");
                return -1;
            }
            else
            {
                appSettings.SW360.ProjectName = sw360ProjectName;
            }
            Logger.Debug("ValidateAppSettings():Validation of SW360 details has completed.");
            return 0;
        }
        public static async Task TriggerFossologyValidation(CommonAppSettings appSettings, ISW360ApicommunicationFacade sW360ApicommunicationFacade)
        {
            ISW360CommonService sw360CommonService = new SW360CommonService(sW360ApicommunicationFacade);
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sW360ApicommunicationFacade, sw360CommonService);
            ISW360Service sw360Service = new Sw360Service(sW360ApicommunicationFacade, sw360CommonService, environmentHelper);
            Logger.Debug("TriggerFossologyValidation(): Starting trigger fossology validation process.");
            try
            {
                ReleasesAllDetails.Sw360Release validRelease = await FindValidRelease(sW360ApicommunicationFacade);

                if (validRelease != null)
                {
                    Logger.Debug("TriggerFossologyValidation(): Valid release found. Triggering Fossology process.");
                    await TriggerFossologyProcessForRelease(validRelease, appSettings, sw360CreatorService);
                }
                else
                {
                    Logger.Debug("TriggerFossologyValidation(): No valid release found. Fossology validation failed.");
                }
                Logger.Debug("TriggerFossologyValidation(): Completed trigger fossology validation process.");
            }
            catch (AggregateException ex)
            {
                Logger.Error($"TriggerFossologyValidation(): An error occurred during the Fossology validation process.{ex.Message}");
                LogHandling.HttpErrorHandelingForLog("Fossology Validation", "TriggerFossologyValidation()", ex, "Check the inner exceptions for more details about the error.");
            }
            catch (Exception ex)
            {
                Logger.Error($"TriggerFossologyValidation(): An unexpected error occurred.{ex.Message}");
                LogHandling.HttpErrorHandelingForLog("Fossology Validation", "TriggerFossologyValidation()", ex, "Investigate the exception details to identify the root cause.");
            }
        }

        private static async Task<ReleasesAllDetails.Sw360Release> FindValidRelease(ISW360ApicommunicationFacade sW360ApicommunicationFacade)
        {
            int page = 0;
            int pageEntries = 40;
            int pageCount = 0;

            while (pageCount < 10)
            {
                ReleasesAllDetails releaseResponse = await GetAllReleasesDetails(sW360ApicommunicationFacade, page, pageEntries);

                if (releaseResponse == null)
                {
                    Logger.Debug($"FindValidRelease(): Fossology token validation failed in SW360 due to release not found");
                    break;
                }

                var validRelease = releaseResponse.Embedded?.Sw360releases?.FirstOrDefault(release =>
                    release?.ClearingState == "APPROVED" &&
                    release?.AllReleasesEmbedded?.Sw360attachments != null &&
                    release.AllReleasesEmbedded.Sw360attachments.Any(attachments =>
                        attachments.Count != 0 &&
                        attachments.Count(attachment => attachment?.AttachmentType == "SOURCE") == 1));

                if (validRelease != null)
                {
                    return validRelease;
                }

                if (!MoveToNextPage(releaseResponse, ref page, ref pageCount))
                {
                    break;
                }
            }

            return null;
        }

        private static bool MoveToNextPage(ReleasesAllDetails releaseResponse, ref int page, ref int pageCount)
        {
            int currentPage = page;
            int totalPages = (int)(releaseResponse?.Page?.TotalPages ?? 0);

            if (currentPage < totalPages - 1)
            {
                page = currentPage + 1;
                pageCount++;
                return true;
            }

            return false;
        }

        private static async Task TriggerFossologyProcessForRelease(ReleasesAllDetails.Sw360Release validRelease, CommonAppSettings appSettings, ISw360CreatorService sw360CreatorService)
        {
            var releaseUrl = validRelease?.Links?.Self?.Href;
            var releaseId = releaseUrl != null ? CommonHelper.GetSubstringOfLastOccurance(releaseUrl, "/") : string.Empty;

            string sw360link = $"{validRelease?.Name}:{validRelease?.Version}:{appSettings?.SW360?.URL}{ApiConstant.Sw360ReleaseUrlApiSuffix}" +
                               $"{releaseId}#/tab-Summary";

            FossTriggerStatus fossResult = await sw360CreatorService.TriggerFossologyProcessForValidation(releaseId, sw360link, environmentHelper);

            if (!string.IsNullOrEmpty(fossResult?.Links?.Self?.Href))
            {
                Logger.Debug($"TriggerFossologyValidation(): SW360 Fossology Process validation successful!!");
            }
        }

        private static async Task<ReleasesAllDetails> GetAllReleasesDetails(ISW360ApicommunicationFacade sW360ApicommunicationFacade, int page, int pageEntries)
        {
            ReleasesAllDetails releaseResponse = null;
            string correlationId = Guid.NewGuid().ToString();
            try
            {
                var responseData = await sW360ApicommunicationFacade.GetAllReleasesWithAllData(page, pageEntries, correlationId);
                LogHandling.LogHttpResponseDetails("Get All Releases Details", $"MethodName:GetAllReleasesDetails(),CorrelationId:{correlationId}", responseData);
                string response = responseData?.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                releaseResponse = JsonConvert.DeserializeObject<ReleasesAllDetails>(response);
            }
            catch (HttpRequestException ex)
            {
                LogHandling.HttpErrorHandelingForLog("Get All Releases Details", $"MethodName:GetAllReleasesDetails(),CorrelationId:{correlationId}", ex, "Investigate the exception details to identify the root cause.");
            }
            catch (InvalidOperationException ex)
            {
                LogHandling.HttpErrorHandelingForLog("Get All Releases Details", $"MethodName:GetAllReleasesDetails(),CorrelationId:{correlationId}", ex, "Investigate the exception details to identify the root cause.");
            }
            catch (UriFormatException ex)
            {
                LogHandling.HttpErrorHandelingForLog("Get All Releases Details", $"MethodName:GetAllReleasesDetails(),CorrelationId:{correlationId}", ex, "Investigate the exception details to identify the root cause.");
            }
            catch (TaskCanceledException ex)
            {
                LogHandling.HttpErrorHandelingForLog("Get All Releases Details", $"MethodName:GetAllReleasesDetails(),CorrelationId:{correlationId}", ex, "Investigate the exception details to identify the root cause.");
            }

            return releaseResponse;
        }
        public static async Task<bool> FossologyUrlValidation(CommonAppSettings appSettings, HttpClient client, IEnvironmentHelper environmentHelper)
        {
            Logger.Debug("FossologyUrlValidation(): Starting Fossology URL validation process.");
            string url = appSettings.SW360.Fossology.URL;
            string correlationId = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(url))
            {
                Logger.Error($"Fossology URL is not provided. Please make sure to add Fossology URL in appsettings.");
                LogHandling.BasicErrorHandelingForLog("Fossology URL Validation", "FossologyUrlValidation", "Fossology URL is not provided. Please ensure the Fossology URL is configured in appsettings.", "Add a valid Fossology URL in the appsettings configuration.");
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
                    try
                    {
                        LogHandling.LogRequestDetails("Fossology URL Validation", $"Methodname:FossologyUrlValidation(),CorrelationId:{correlationId}", client, url);
                        HttpResponseMessage response = await client.GetAsync(new Uri(appSettings.SW360.Fossology.URL));
                        LogHandling.LogHttpResponseDetails("Fossology URL Validation", $"Methodname:FossologyUrlValidation(),CorrelationId:{correlationId}", response);

                        if (response.IsSuccessStatusCode)
                        {
                            // Fossology URL is valid
                            Logger.Debug($"FossologyUrlValidation(): Fossology URL validation successful.");
                            Logger.Debug("FossologyUrlValidation(): Completed Fossology URL validation process.");
                            return true;
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable) 
                        {
                            Logger.Error($"Fossology URL is not working due to {response.ReasonPhrase}");
                            LogHandling.HttpErrorHandelingForLog("Fossology URL Validation", $"Methodname:FossologyUrlValidation(),CorrelationId:{correlationId}", new Exception($"Fossology URL not working. Received HTTP status code: {response.StatusCode}. URL: {url}"), $"Ensure the Fossology URL is accessible and returns a successful response. URL: {url}");
                            environmentHelper.CallEnvironmentExit(-1);
                        }
                        else
                        {
                            // Fossology URL is not valid
                            Logger.Error($"Fossology URL is not valid. Please make sure to add a valid Fossology URL in appsettings.");
                            LogHandling.HttpErrorHandelingForLog("Fossology URL Validation", $"Methodname:FossologyUrlValidation(),CorrelationId:{correlationId}", new Exception($"Fossology URL not working. Received HTTP status code: {response.StatusCode}. URL: {url}"), $"Ensure the Fossology URL is accessible and returns a successful response. URL: {url}");
                            environmentHelper.CallEnvironmentExit(-1);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        // Fossology URL is not valid
                        Logger.Error($"Fossology URL is not working. Please check and try again.");
                        LogHandling.HttpErrorHandelingForLog("Fossology URL Validation", $"Methodname:FossologyUrlValidation(),CorrelationId:{correlationId}", ex, "Check the network connection and ensure the Fossology server is reachable.");
                        environmentHelper.CallEnvironmentExit(-1);
                    }
                }
                else
                {
                    Logger.Debug($"FossologyUrlValidation(): Fossology URL is not valid.");
                    LogHandling.BasicErrorHandelingForLog("Fossology URL Validation", $"Methodname:FossologyUrlValidation()", $"Fossology URL does not match the expected production or staging URLs. URL: {url}", "Ensure the Fossology URL matches the configured production or staging URLs.");
                    environmentHelper.CallEnvironmentExit(-1);
                }
            }
            else
            {
                Logger.Error($"Fossology URL is not valid. Please make sure to add a valid Fossology URL in appsettings.");
                LogHandling.BasicErrorHandelingForLog("Fossology URL Validation", $"Methodname:FossologyUrlValidation()", "The provided Fossology URL is not a valid absolute URI.", "Check the Fossology URL format in the appsettings configuration.");
                environmentHelper.CallEnvironmentExit(-1);
            }

            Logger.Debug("FossologyUrlValidation(): Completed Fossology URL validation process with failure.");
            return false;
        }

    }
}
