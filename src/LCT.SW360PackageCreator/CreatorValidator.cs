// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Services.Interface;
using System.IO;
using System.Threading.Tasks;
using LCT.Common;
using LCT.APICommunications.Model;
using log4net;
using System.Reflection;
using LCT.Common.Interface;
using LCT.APICommunications.Model.Foss;
using LCT.APICommunications;
using LCT.Facade.Interfaces;
using LCT.Services;
using log4net.Core;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using LCT.SW360PackageCreator.Model;
using System.Linq;
using LCT.Common.Constants;


namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// Validates the creator param
    /// </summary>
    public static class CreatorValidator
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static IEnvironmentHelper environmentHelper;
        public static async Task<int> ValidateAppSettings(CommonAppSettings appSettings, ISw360ProjectService sw360ProjectService, ProjectReleases projectReleases)
        {
            string sw360ProjectName = await sw360ProjectService.GetProjectNameByProjectIDFromSW360(appSettings.SW360.ProjectID, appSettings.SW360.ProjectName,projectReleases);

            if (string.IsNullOrEmpty(sw360ProjectName))
            {
                throw new InvalidDataException($"Invalid Project Id - {appSettings.SW360.ProjectID}");
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
            IEnvironmentHelper environmentHelper = new EnvironmentHelper();
            ISW360CommonService sw360CommonService = new SW360CommonService(sW360ApicommunicationFacade);
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sW360ApicommunicationFacade, sw360CommonService);
            ISW360Service sw360Service = new Sw360Service(sW360ApicommunicationFacade, sw360CommonService, environmentHelper);
            
            try
            {
                string page = "0";
                string pageEntries = "20";
                bool validReleaseFound = false;
                ReleasesAllDetails.Sw360Release validRelease = null;
                int pageCount = 0;
                while (!validReleaseFound && pageCount < 10)
                {
                    HttpResponseMessage responseData = await sW360ApicommunicationFacade.GetAllReleasesWithAllData(page, pageEntries);
                    string response = responseData?.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                    ReleasesAllDetails releaseResponse = JsonConvert.DeserializeObject<ReleasesAllDetails>(response);

                    validRelease = releaseResponse._embedded.sw360releases
                        .FirstOrDefault(release => release.clearingState == "APPROVED" &&
                                                   release._embedded?.sw360attachments != null &&
                                                   release._embedded.sw360attachments.Any(attachments => attachments.Count != 0));

                    if (validRelease != null)
                    {
                        validReleaseFound = true;
                    }
                    else
                    {
                        // Check if there are more pages
                        int currentPage = int.Parse(page);
                        int totalPages = releaseResponse.page.totalPages;
                        if (currentPage < totalPages - 1)
                        {
                            page = (currentPage + 1).ToString();
                            pageCount++;
                        }
                        else
                        {
                            break; // No more pages to check
                        }
                    }
                }

                if (validReleaseFound)
                {
                    var releaseUrl = validRelease._links.self.href;
                    var releaseId = CommonHelper.GetSubstringOfLastOccurance(releaseUrl, "/");
                    string sw360link = $"{validRelease.name}:{validRelease.version}:{appSettings.SW360.URL}{ApiConstant.Sw360ReleaseUrlApiSuffix}" +
                    $"{releaseId}#/tab-Summary";
                        FossTriggerStatus fossResult = await sw360CreatorService.TriggerFossologyProcessForValidation(releaseId, sw360link);
                    if (!string.IsNullOrEmpty(fossResult?.Links?.Self?.Href))
                        {
                            Logger.Logger.Log(null, Level.Info, $"SW360 Fossology Process validation successfull!!", null);
                        }
                }
                
            }
            catch (AggregateException ex)
            {
                Logger.Debug($"\tError in TriggerFossologyProcess--{ex}");
                Logger.Error($"Trigger Fossology Process failed.Please check fossology configuration in sw360");
                environmentHelper.CallEnvironmentExit(-1);
            }
        }
        public static async Task<bool> FossologyUrlValidation(CommonAppSettings appSettings, HttpClient client, IEnvironmentHelper environmentHelper)
        {
            string url = appSettings.SW360.Fossology.URL.ToLower();
            string prodFossUrl = Dataconstant.Cdx_ProductionFossologyURL.ToLower();
            string stageFossUrl = Dataconstant.Cdx_StageFossologyURL.ToLower();

            if (string.IsNullOrEmpty(appSettings.SW360.Fossology.URL))
            {
                Logger.Error($"Fossology URL is not provided ,Please make sure to add Fossologyurl in appsettings..");
                Logger.Debug($"FossologyUrlValidation() : Fossology url not provided in appsettings");
                environmentHelper.CallEnvironmentExit(-1);
            }
            else if (Uri.IsWellFormedUriString(appSettings.SW360.Fossology.URL, UriKind.Absolute))
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
