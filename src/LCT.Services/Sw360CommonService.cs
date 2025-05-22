// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Facade.Interfaces;
using LCT.Services.Interface;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.Services
{
    /// <summary>
    /// The class SW360CommonService provides the common services
    /// </summary>
    public class SW360CommonService : ISW360CommonService
    {

        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ISW360ApicommunicationFacade m_SW360ApiCommunicationFacade;
        private readonly List<string> externalIdKeyList = new List<string>() { "?package-url=", "?purl.id=" };

        #region constructor

        /// <summary>
        /// constructor for the class SW360CommonService
        /// </summary>
        /// <param name="sw360ApiCommunicationFacade"></param>
        public SW360CommonService(ISW360ApicommunicationFacade sw360ApiCommunicationFacade)
        {
            m_SW360ApiCommunicationFacade = sw360ApiCommunicationFacade;
        }

        #endregion


        /// <summary>
        /// Gets the componet data by component external id
        /// </summary>
        /// <param name="componentName">componentName</param>
        /// <param name="componentExternalId">componentExternalId</param>
        /// <param name="isComponentExist">isComponentExist</param>
        /// <returns>Sw360Components</returns>
        public async Task<ComponentStatus> GetComponentDataByExternalId(string componentName, string componentExternalId)
        {
            Logger.Debug($"GetComponentDataByExternalId(): Starting to identifying Component through External Id - Name-{componentName},ExternalId-{componentExternalId}");
            string externalIdUriString;
            if (componentExternalId.Contains(Dataconstant.PurlCheck()["NPM"]))
            {
                externalIdUriString = Uri.EscapeDataString(componentExternalId);
            }
            else
            {
                externalIdUriString = componentExternalId;
            }
            ComponentStatus sw360components = new ComponentStatus();
            sw360components.isComponentExist = false;

            try
            {
                foreach (string externalIdKey in externalIdKeyList)
                {
                    var sw360ComponentsList = await GetCompListFromExternalIDCombinations(externalIdUriString, externalIdKey);
                    if (sw360ComponentsList.Count == 0 && externalIdUriString.Contains(Dataconstant.PurlCheck()["DEBIAN"]))
                    {
                        string NewExternalIdUriString = Uri.EscapeDataString(componentExternalId.Replace("?arch=source", ""));
                        sw360ComponentsList = await GetCompListFromExternalIDCombinations(NewExternalIdUriString, externalIdKey);
                    }

                    if (sw360ComponentsList.Count > 0)
                    {
                        sw360components = GetComponentExistStatus(componentName, externalIdKey, sw360ComponentsList);

                        if (sw360components.isComponentExist)
                        {
                            break;
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                sw360components.isComponentExist = false;
                LogHandlingHelper.ExceptionErrorHandling("GetReleaseDataByExternalId",$"MethodName:GetComponentDataByExternalId(), ComponentName:{componentName}, componentExternalId:{componentExternalId}",ex,"An HTTP request error occurred while trying to fetch release data. ");
                Logger.Error($"GetComponentDataByExternalId():", ex);
            }
            catch (AggregateException ex)
            {
                sw360components.isComponentExist = false;
                LogHandlingHelper.ExceptionErrorHandling("GetComponentDataByExternalId",$"MethodName:GetComponentDataByExternalId(), ComponentName:{componentName}, componentExternalId:{componentExternalId}",ex,"Multiple errors occurred while processing the request. Please investigate the inner exceptions for more details.");
                Logger.Error($"GetComponentDataByExternalId():", ex);
            }
            Logger.Debug($"GetComponentDataByExternalId(): Completed to identifying Component through External Id - Name-{componentName},ExternalId-{componentExternalId}");
            return sw360components;
        }

        private async Task<IList<Sw360Components>> GetCompListFromExternalIDCombinations(string externalIdUriString, string externalIdKey)
        {
            string correlationId = Guid.NewGuid().ToString();
            HttpResponseMessage httpResponseComponent = await m_SW360ApiCommunicationFacade.GetComponentByExternalId(externalIdUriString, externalIdKey, correlationId);
            await LogHandlingHelper.HttpResponseHandling("Response of get component data by externalId", $"MethodName:GetReleaseDataByExternalId(),CorrelationId:{correlationId}", httpResponseComponent);
            var responseContent = httpResponseComponent?.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
            var componentsModel = JsonConvert.DeserializeObject<ComponentsModel>(responseContent);
            return componentsModel?.Embedded?.Sw360components ?? new List<Sw360Components>();
        }

        /// <summary>
        /// Gets the release data by release external id
        /// </summary>
        /// <param name="releaseName">releaseName</param>
        /// <param name="releaseVersion">releaseVersion</param>
        /// <param name="releaseExternalId">releaseExternalId</param>
        /// <param name="isReleaseExist">isReleaseExist</param>
        /// <returns>Sw360Releases</returns>
        public async Task<Releasestatus> GetReleaseDataByExternalId(string releaseName, string releaseVersion, string releaseExternalId)
        {
            Logger.Debug($"GetReleaseDataByExternalId(): Identifying release data through ExternalId, Release details - {releaseName}@{releaseVersion}");
            Releasestatus releasestatus = new Releasestatus();
            releasestatus.isReleaseExist = false;
            string correlationId = Guid.NewGuid().ToString();
            try
            {
                foreach (string externalIdKey in externalIdKeyList)
                {
                    HttpResponseMessage httpResponseComponent = await m_SW360ApiCommunicationFacade.GetReleaseByExternalId(releaseExternalId, externalIdKey, correlationId);
                    await LogHandlingHelper.HttpResponseHandling("Response of get release data by externalId", $"MethodName:GetReleaseDataByExternalId(),CorrelationId:{correlationId}", httpResponseComponent);
                    var responseContent = httpResponseComponent?.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                    var componentsRelease = JsonConvert.DeserializeObject<ComponentsRelease>(responseContent);
                    var sw360releasesdata = componentsRelease?.Embedded?.Sw360Releases ?? new List<Sw360Releases>();

                    //It's for Local Sw360 servers,making an API call with EscapeDataString..
                    if (sw360releasesdata.Count == 0 && releaseExternalId.Contains(Dataconstant.PurlCheck()["NPM"]))
                    {
                        Logger.Debug($"GetReleaseDataByExternalId(): If releaseExternalId have NPM . We reruning the api call.");
                        releaseExternalId = Uri.EscapeDataString(releaseExternalId);
                        httpResponseComponent = await m_SW360ApiCommunicationFacade.GetReleaseByExternalId(releaseExternalId, externalIdKey, correlationId);
                        await LogHandlingHelper.HttpResponseHandling("Response of get release data by externalId", $"MethodName:GetReleaseDataByExternalId(),CorrelationId:{correlationId}", httpResponseComponent);
                        responseContent = httpResponseComponent?.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                        componentsRelease = JsonConvert.DeserializeObject<ComponentsRelease>(responseContent);
                        sw360releasesdata = componentsRelease?.Embedded?.Sw360Releases ?? new List<Sw360Releases>();
                    }

                    if (sw360releasesdata.Count > 0)
                    {
                        Releasestatus releaseStatus = GetReleaseExistStatus(releaseName, externalIdKey, sw360releasesdata);
                        if (releaseStatus.isReleaseExist)
                        {
                            releasestatus.sw360Releases = releaseStatus.sw360Releases;
                            releasestatus.isReleaseExist = releaseStatus.isReleaseExist;
                            break;
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                releasestatus.isReleaseExist = false;
                LogHandlingHelper.ExceptionErrorHandling("GetReleaseDataByExternalId", $"MethodName:GetReleaseDataByExternalId(), ReleaseName:{releaseName}, ReleaseVersion:{releaseVersion}, ReleaseExternalId:{releaseExternalId}", ex, "An HTTP request error occurred while trying to fetch release data.");
                Logger.Error($"GetReleaseDataByExternalId():", ex);
            }
            catch (AggregateException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetReleaseDataByExternalId", $"MethodName:GetReleaseDataByExternalId(), ReleaseName:{releaseName}, ReleaseVersion:{releaseVersion}, ReleaseExternalId:{releaseExternalId}", ex, "Multiple errors occurred while processing the request. Please investigate the inner exceptions for more details.");
                Logger.Error($"GetReleaseDataByExternalId():", ex);
            }
            catch (JsonReaderException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetReleaseDataByExternalId", $"MethodName:GetReleaseDataByExternalId(), ReleaseName:{releaseName}, ReleaseVersion:{releaseVersion}, ReleaseExternalId:{releaseExternalId}", ex, "A JSON parsing error occurred while deserializing the response. Ensure the response format is correct and matches the expected structure.");
                Logger.Error($"GetReleaseDataByExternalId():JsonReaderException", ex);
            }

            return releasestatus;
        }

        /// <summary>
        /// Gets the ReleaseId By using the ComponentId & version
        /// </summary>
        /// <param name="componentId">componentId</param>
        /// <param name="componentVersion">componentVersion</param>
        /// <returns>string</returns>
        public async Task<string> GetReleaseIdByComponentId(string componentId, string componentVersion)
        {
            string releaseId = string.Empty;
            string correlationId = Guid.NewGuid().ToString();
            try
            {
                string releaseResponseBody = await m_SW360ApiCommunicationFacade.GetReleaseOfComponentById(componentId, correlationId);
                LogHandlingHelper.HttpResponseOfStringContent("Get Release Id By ComponentId", $"MethodName:GetReleaseIdByComponentId(),CorrelationId:{correlationId}", releaseResponseBody);
                var responseData = JsonConvert.DeserializeObject<ReleaseIdOfComponent>(releaseResponseBody);
                var listofSw360Releases = responseData?.Embedded?.Sw360Releases ?? new List<Sw360Releases>();
                for (int i = 0; i < listofSw360Releases.Count; i++)
                {
                    if (listofSw360Releases[i].Version?.ToLowerInvariant() == componentVersion.ToLowerInvariant())
                    {
                        string urlofreleaseid = listofSw360Releases[i]?.Links?.Self?.Href ?? string.Empty;
                        releaseId = CommonHelper.GetSubstringOfLastOccurance(urlofreleaseid, "/");
                    }
                }
            }
            catch (HttpRequestException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get ReleaseId By ComponentId", $"MethodName:GetReleaseIdByComponentId()", e, "An HTTP request error occurred while trying to fetch release data.");
                Logger.Error("GetReleaseIdByComponentId():", e);
                Environment.ExitCode = -1;
            }
            catch (AggregateException e)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get ReleaseId By ComponentId", $"MethodName:GetReleaseIdByComponentId()", e, "Multiple errors occurred while processing the request. Please investigate the inner exceptions for more details.");
                Logger.Error("GetReleaseIdByComponentId():", e);
                Environment.ExitCode = -1;
            }

            return releaseId;
        }

        #region PrivateMethods

        private static Releasestatus GetReleaseExistStatus(
            string name, string externlaIdKey, IList<Sw360Releases> sw360releasesdata)
        {
            Dictionary<int, Sw360Releases> releaseCollection = new Dictionary<int, Sw360Releases>();

            Logger.Debug($"GetReleaseExistStatus(): Identifying release exist status from SW360: {name}");
            foreach (var release in sw360releasesdata)
            {
                string packageUrl = string.Empty;
                packageUrl = GetPackageUrlValue(externlaIdKey, release, packageUrl);                
                try
                {
                    var purlids = JsonConvert.DeserializeObject<List<string>>(packageUrl);

                    if (releaseCollection.ContainsKey(purlids.Count) && releaseCollection[purlids.Count].Name.ToLower().Equals(name.ToLower()))
                    {
                        // Do nothing
                    }
                    else if (releaseCollection.ContainsKey(purlids.Count))
                    {
                        releaseCollection[1] = release;
                    }
                    else
                    {
                        releaseCollection.Add(purlids.Count, release);
                    }
                }
                catch (JsonReaderException)
                {
                    UpdateCollection(name, ref releaseCollection, release);
                }
            }
            Releasestatus releasestatus = new Releasestatus();
            releasestatus.sw360Releases = releaseCollection[releaseCollection.Keys.Max()];
            Logger.Debug($"GetReleaseExistStatus(): Selected release '{releasestatus.sw360Releases?.Name}' for the name '{name}' based on the highest number of PURL IDs.\n");
            releasestatus.isReleaseExist = !string.IsNullOrEmpty(releasestatus.sw360Releases?.ExternalIds?.Package_Url) || !string.IsNullOrEmpty(releasestatus.sw360Releases?.ExternalIds?.Purl_Id);

            return releasestatus;
        }


        private static ComponentStatus GetComponentExistStatus(string name, string externlaIdKey, IList<Sw360Components> sw360components)
        {
            Dictionary<int, Sw360Components> componentCollection = new Dictionary<int, Sw360Components>();

            Logger.Debug($"GetComponentExistStatus(): Identifying component exist status from SW360 : {name}");
            foreach (var componentsData in sw360components)
            {
                string packageUrl = string.Empty;
                packageUrl = GetPackageUrlValue(externlaIdKey, componentsData, packageUrl);
                Logger.Debug($"GetComponentExistStatus(): Component Name : {name} from {packageUrl}");
                try
                {
                    var purlids = JsonConvert.DeserializeObject<List<string>>(packageUrl);

                    if (componentCollection.ContainsKey(purlids.Count) && componentCollection[purlids.Count].Name.ToLower().Equals(name.ToLower()))
                    {
                        // Do nothing
                    }
                    else if (componentCollection.ContainsKey(purlids.Count))
                    {
                        componentCollection[1] = componentsData;
                    }
                    else
                    {
                        componentCollection.Add(purlids.Count, componentsData);
                    }
                }
                catch (JsonReaderException)
                {
                    UpdateCollection(name, ref componentCollection, componentsData);
                }
            }
            ComponentStatus component = new ComponentStatus();

            component.Sw360components = componentCollection[componentCollection.Keys.Max()];
            Logger.Debug($"GetComponentExistStatus(): Component Name : {name} selected {component.Sw360components?.Name},based on the highest number of PURL IDs \n");
            component.isComponentExist = !string.IsNullOrEmpty(component.Sw360components?.ExternalIds?.Package_Url) || !string.IsNullOrEmpty(component.Sw360components?.ExternalIds?.Purl_Id);
            return component;

        }


        private static string GetPackageUrlValue(string externlaIdKey, Sw360Releases release, string packageUrl)
        {
            if (externlaIdKey.Contains("purl"))
            {
                packageUrl = release.ExternalIds?.Purl_Id;
            }

            else if (externlaIdKey.Contains("package"))
            {
                packageUrl = release.ExternalIds?.Package_Url;
            }
            else
            {
                // do nothing
            }

            return packageUrl;
        }

        private static string GetPackageUrlValue(string externlaIdKey, Sw360Components components, string packageUrl)
        {
            if (externlaIdKey.Contains("purl"))
            {
                packageUrl = components.ExternalIds?.Purl_Id;
            }

            else if (externlaIdKey.Contains("package"))
            {
                packageUrl = components.ExternalIds?.Package_Url;
            }
            else
            {
                // do nothing
            }

            return packageUrl;
        }

        private static void UpdateCollection(string name, ref Dictionary<int, Sw360Components> componentCollection, Sw360Components components)
        {
            if (componentCollection.ContainsKey(1) && componentCollection[1].Name.ToLower().Equals(name.ToLower()))
            {
                // Do nothing
            }
            else if (componentCollection.ContainsKey(1))
            {
                componentCollection[1] = components;
            }
            else
            {
                componentCollection.Add(1, components);
            }
        }

        private static void UpdateCollection(string name, ref Dictionary<int, Sw360Releases> releaseCollection, Sw360Releases release)
        {
            if (releaseCollection.ContainsKey(1) && releaseCollection[1].Name.ToLower().Equals(name.ToLower()))
            {
                // Do nothing
            }
            else if (releaseCollection.ContainsKey(1))
            {
                releaseCollection[1] = release;
            }
            else
            {
                releaseCollection.Add(1, release);
            }
        }

        #endregion
    }
}
