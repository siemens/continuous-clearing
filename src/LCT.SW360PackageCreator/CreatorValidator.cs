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
            string sw360ProjectName = await sw360ProjectService.GetProjectNameByProjectIDFromSW360(appSettings.SW360.ProjectID, appSettings.SW360.ProjectName, projectReleases);

            if (string.IsNullOrEmpty(sw360ProjectName))
            {
                throw new InvalidDataException($"Invalid Project Id - {appSettings.SW360.ProjectID}");
            }
            else if (CommonHelper.ValidateProjectName(sw360ProjectName, projectReleases.Name) == -1)
            {
                return -1;
            }
            else if (projectReleases?.clearingState == "CLOSED")
            {
                Logger.Error($"Provided Sw360 project is not in active state ,Please make sure you added the correct project details that is in active state..");
                Logger.Debug($"ValidateAppSettings() : Sw360 project " + projectReleases.Name + " is in " + projectReleases.clearingState + " state.");
                return -1;
            }
            else
            {
                appSettings.SW360.ProjectName = sw360ProjectName;
            }
            return 0;
        }
        public static async Task TriggerFossologyValidation(CommonAppSettings appSettings, ISW360ApicommunicationFacade sW360ApicommunicationFacade)
        {
            ISW360CommonService sw360CommonService = new SW360CommonService(sW360ApicommunicationFacade);
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sW360ApicommunicationFacade, sw360CommonService);
            ISW360Service sw360Service = new Sw360Service(sW360ApicommunicationFacade, sw360CommonService, environmentHelper);

            try
            {
                ReleasesAllDetails.Sw360Release validRelease = await FindValidRelease(sW360ApicommunicationFacade);

                if (validRelease != null)
                {
                    await TriggerFossologyProcessForRelease(validRelease, appSettings, sw360CreatorService);
                }
                else
                {
                    Logger.Debug($"TriggerFossologyValidation(): Fossology URL validation failed");
                }
            }
            catch (AggregateException ex)
            {
                Logger.Debug($"\tError in TriggerFossologyValidation--{ex}");
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

            FossTriggerStatus fossResult = await sw360CreatorService.TriggerFossologyProcessForValidation(releaseId, sw360link);

            if (!string.IsNullOrEmpty(fossResult?.Links?.Self?.Href))
            {
                Logger.Debug($"TriggerFossologyValidation(): SW360 Fossology Process validation successful!!");
            }
        }

        private static async Task<ReleasesAllDetails> GetAllReleasesDetails(ISW360ApicommunicationFacade sW360ApicommunicationFacade, int page, int pageEntries)
        {
            ReleasesAllDetails releaseResponse = null;
            try
            {
                var responseData = await sW360ApicommunicationFacade.GetAllReleasesWithAllData(page, pageEntries);
                string response = responseData?.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                releaseResponse = JsonConvert.DeserializeObject<ReleasesAllDetails>(response);
            }
            catch (HttpRequestException ex)
            {
                Logger.Debug($"GetAllReleasesDetails():", ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Debug($"GetAllReleasesDetails():", ex);
            }
            catch (UriFormatException ex)
            {
                Logger.Debug($"GetAllReleasesDetails():", ex);
            }
            catch (TaskCanceledException ex)
            {
                Logger.Debug($"GetAllReleasesDetails():", ex);
            }

            return releaseResponse;
        }
        public static async Task<bool> FossologyUrlValidation(CommonAppSettings appSettings, HttpClient client, IEnvironmentHelper environmentHelper)
        {
            string url = appSettings.SW360.Fossology.URL;
            if (string.IsNullOrEmpty(url))
            {
                Logger.Error($"Fossology URL is not provided, Please make sure to add Fossology URL in appsettings.");
                Logger.Debug($"FossologyUrlValidation() : Fossology URL not provided in appsettings");
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
                        HttpResponseMessage response = await client.GetAsync(new Uri(appSettings.SW360.Fossology.URL));
                        if (response.IsSuccessStatusCode)
                        {
                            // Fossology URL is valid                                   
                            return true;
                        }
                        else
                        {
                            // Fossology URL is not valid                                   
                            Logger.Error($"Fossology URL is not valid ,please make sure to add valid fossologyurl in appsettings..");
                            Logger.Debug($"FossologyUrlValidation() : Fossology URL is not valid.");
                            environmentHelper.CallEnvironmentExit(-1);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        // Fossology URL is not valid                                   
                        Logger.Error($"Fossology URL is not working ,please check once try again....");
                        Logger.Debug($"FossologyUrlValidation() : Fossology URL is not valid.{ex}");
                        environmentHelper.CallEnvironmentExit(-1);
                    }
                }
                else
                {
                    Logger.Debug($"FossologyUrlValidation() : Fossology URL is not valid");
                    Logger.Error($"Fossology URL is not valid ,please check once try again....");
                    environmentHelper.CallEnvironmentExit(-1);
                }
            }
            else
            {
                Logger.Error($"Fossology URL is not provided ,Please make sure to add fossologyurl in appsettings..");
                Logger.Debug($"FossologyUrlValidation() : Fossologyurl not provided in appsettings");
                environmentHelper.CallEnvironmentExit(-1);
            }
            return false;
        }

    }
}
