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
            ReleasesInfo releasesInfo = new ReleasesInfo();

            try
            {
                string responseBody = await sW360ApicommunicationFacade.GetReleases();
                var modelMappedObject = JsonConvert.DeserializeObject<ComponentsRelease>(responseBody);
                var releaseId = string.Empty;
                if (modelMappedObject != null && modelMappedObject.Embedded?.Sw360Releases?.Count > 0)
                {
                    foreach (var ReleaseidList in modelMappedObject.Embedded?.Sw360Releases)
                    {
                        var releaseUrl = ReleaseidList.Links?.Self?.Href;
                        releasesInfo = await CallApiAsync(releaseUrl, sw360Service);
                        if (releasesInfo != null)
                        {
                            releaseId = CommonHelper.GetSubstringOfLastOccurance(releaseUrl, "/");
                            break;
                        }
                    }
                }
                else
                {
                    Logger.Debug($"TriggerFossologyValidation():Trigger Fossology Process validation failed");
                    Logger.Error($"Trigger Fossology Process validation failed");
                    environmentHelper.CallEnvironmentExit(-1);
                }
                if (releasesInfo != null)
                {
                    string sw360link = $"{releasesInfo.Name}:{releasesInfo.Version}:{appSettings.SW360.URL}{ApiConstant.Sw360ReleaseUrlApiSuffix}" +
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
        private static async Task<ReleasesInfo> CallApiAsync(string endpoint, ISW360Service sw360Service)
        {
            try
            {
                ReleasesInfo releasesInfo = await sw360Service.GetReleaseDataOfComponent(endpoint);
                var clearingState = releasesInfo.ClearingState?.ToString();
                if (clearingState == "APPROVED")
                {
                    // Return the result if the clearingState is "approved"
                    return releasesInfo;
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"CallApiAsync():{ex}");
            }

            return null;
        }
    }
}
