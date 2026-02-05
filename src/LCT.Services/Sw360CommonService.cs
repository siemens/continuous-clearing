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
    /// <remarks>
    /// constructor for the class SW360CommonService
    /// </remarks>
    /// <param name="sw360ApiCommunicationFacade"></param>
    public class SW360CommonService(ISW360ApicommunicationFacade sw360ApiCommunicationFacade) : ISW360CommonService
    {

        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ISW360ApicommunicationFacade m_SW360ApiCommunicationFacade = sw360ApiCommunicationFacade;
        private readonly List<string> externalIdKeyList = new List<string>() { "?package-url=", "?purl.id=" };

        #region constructor

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
            Logger.DebugFormat("GetComponentDataByExternalId(): Starting to identifying Component through External Id - Name-{0},ExternalId-{1}", componentName, componentExternalId);
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
                LogHandlingHelper.ExceptionErrorHandling("Exception while getting Component Data By ExternalId", $"MethodName:GetComponentDataByExternalId(), ComponentName:{componentName}, componentExternalId:{componentExternalId}", ex, "An HTTP request error occurred while trying to fetch release data. ");
                Logger.Error("GetComponentDataByExternalId():", ex);
            }
            catch (AggregateException ex)
            {
                sw360components.isComponentExist = false;
                LogHandlingHelper.ExceptionErrorHandling("AggregateException while getting Component Data By ExternalId", $"MethodName:GetComponentDataByExternalId(), ComponentName:{componentName}, componentExternalId:{componentExternalId}", ex, "Multiple errors occurred while processing the request. Please investigate the inner exceptions for more details.");
                Logger.Error("GetComponentDataByExternalId():", ex);
            }

            return sw360components;
        }

        /// <summary>
        /// Retrieves a list of SW360 components that match the specified external ID combination.
        /// </summary>       
        /// <param name="externalIdUriString">The URI string representing the external ID value used to query components. Cannot be null or empty.</param>
        /// <param name="externalIdKey">The key identifying the type of external ID to match against components. Cannot be null or empty.</param>
        /// <returns>A list of SW360 components that correspond to the given external ID combination. Returns an empty list if no
        /// matching components are found.</returns>
        private async Task<IList<Sw360Components>> GetCompListFromExternalIDCombinations(string externalIdUriString, string externalIdKey)
        {
            HttpResponseMessage httpResponseComponent = await m_SW360ApiCommunicationFacade.GetComponentByExternalId(externalIdUriString, externalIdKey);
            await LogHandlingHelper.HttpResponseHandling("Response of get component data by externalId", $"MethodName:GetReleaseDataByExternalId()", httpResponseComponent);
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
            Logger.DebugFormat("GetReleaseDataByExternalId(): Identifying release data through ExternalId, Release details - {0}@{1}", releaseName, releaseVersion);
            Releasestatus releasestatus = new Releasestatus();

            releasestatus.isReleaseExist = false;

            try
            {
                foreach (string externalIdKey in externalIdKeyList)
                {
                    HttpResponseMessage httpResponseComponent = await m_SW360ApiCommunicationFacade.GetReleaseByExternalId(releaseExternalId, externalIdKey);
                    await LogHandlingHelper.HttpResponseHandling("Response of get release data by externalId", $"MethodName:GetReleaseDataByExternalId()", httpResponseComponent);
                    var responseContent = httpResponseComponent?.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                    var componentsRelease = JsonConvert.DeserializeObject<ComponentsRelease>(responseContent);
                    var sw360releasesdata = componentsRelease?.Embedded?.Sw360Releases ?? new List<Sw360Releases>();

                    //It's for Local Sw360 servers,making an API call with EscapeDataString..
                    if (sw360releasesdata.Count == 0 && (releaseExternalId.Contains(Dataconstant.PurlCheck()["NPM"]) || releaseExternalId.Contains(Dataconstant.PurlCheck()["DEBIAN"]) || releaseExternalId.Contains(Dataconstant.PurlCheck()["ALPINE"])))
                    {
                        Logger.Debug("GetReleaseDataByExternalId(): If releaseExternalId have NPM or Debian or Alpine . We reruning the api call.");
                        releaseExternalId = Uri.EscapeDataString(releaseExternalId);
                        httpResponseComponent = await m_SW360ApiCommunicationFacade.GetReleaseByExternalId(releaseExternalId, externalIdKey);
                        await LogHandlingHelper.HttpResponseHandling("Response of get release data by externalId", $"MethodName:GetReleaseDataByExternalId()", httpResponseComponent);
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
                LogHandlingHelper.ExceptionErrorHandling("Exception while getting Release Data By ExternalId", $"MethodName:GetReleaseDataByExternalId(), ReleaseName:{releaseName}, ReleaseVersion:{releaseVersion}, ReleaseExternalId:{releaseExternalId}", ex, "An HTTP request error occurred while trying to fetch release data.");
                Logger.Error("GetReleaseDataByExternalId():", ex);
            }
            catch (AggregateException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("AggregateException while getting Component Data By ExternalId", $"MethodName:GetReleaseDataByExternalId(), ReleaseName:{releaseName}, ReleaseVersion:{releaseVersion}, ReleaseExternalId:{releaseExternalId}", ex, "Multiple errors occurred while processing the request. Please investigate the inner exceptions for more details.");
                Logger.Error("GetReleaseDataByExternalId():", ex);
            }
            catch (JsonReaderException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("JsonReaderException while getting Component Data By ExternalId", $"MethodName:GetReleaseDataByExternalId(), ReleaseName:{releaseName}, ReleaseVersion:{releaseVersion}, ReleaseExternalId:{releaseExternalId}", ex, "A JSON parsing error occurred while deserializing the response. Ensure the response format is correct and matches the expected structure.");
                Logger.Error("GetReleaseDataByExternalId():JsonReaderException", ex);
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
            try
            {
                string releaseResponseBody = await m_SW360ApiCommunicationFacade.GetReleaseOfComponentById(componentId);
                LogHandlingHelper.HttpResponseOfStringContent("Get Release Id By ComponentId", $"MethodName:GetReleaseIdByComponentId()", releaseResponseBody);
                var responseData = JsonConvert.DeserializeObject<ReleaseIdOfComponent>(releaseResponseBody);
                var listofSw360Releases = responseData?.Embedded?.Sw360Releases ?? new List<Sw360Releases>();
                for (int i = 0; i < listofSw360Releases.Count; i++)
                {
                    if (listofSw360Releases[i].Version != null && listofSw360Releases[i].Version.Equals(componentVersion, StringComparison.InvariantCultureIgnoreCase))
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

        /// <summary>
        /// Determines the existence status of a release by matching the specified release name and external ID key
        /// against a collection of SW360 release data.
        /// </summary>
        /// <param name="name">The name of the release to search for within the provided release data. Cannot be null.</param>
        /// <param name="externlaIdKey">The external ID key used to identify and match releases. Cannot be null.</param>
        /// <param name="sw360releasesdata">A collection of SW360 release data to be searched. Cannot be null and must contain at least one element.</param>
        /// <returns>A Releasestatus object containing information about the matched release and its existence status. The
        /// returned object will indicate whether a release with the specified name and external ID exists in the
        /// provided data.</returns>
        private static Releasestatus GetReleaseExistStatus(
            string name, string externlaIdKey, IList<Sw360Releases> sw360releasesdata)
        {
            Dictionary<int, Sw360Releases> releaseCollection = new Dictionary<int, Sw360Releases>();

            Logger.DebugFormat("GetReleaseExistStatus(): Identifying release exist status from SW360: {0}", name);
            foreach (var release in sw360releasesdata)
            {
                string packageUrl = string.Empty;
                packageUrl = GetPackageUrlValue(externlaIdKey, release, packageUrl);
                try
                {
                    var purlids = JsonConvert.DeserializeObject<List<string>>(packageUrl);

                    if (releaseCollection.TryGetValue(purlids.Count, out Sw360Releases value) && value.Name.ToLower().Equals(name.ToLower()))
                    {
                        // Do nothing
                    }
                    else if (!releaseCollection.TryAdd(purlids.Count, release))
                    {
                        releaseCollection[1] = release;
                    }
                }
                catch (JsonReaderException)
                {
                    UpdateCollection(name, ref releaseCollection, release);
                }
            }
            Releasestatus releasestatus = new Releasestatus();
            releasestatus.sw360Releases = releaseCollection[releaseCollection.Keys.Max()];
            Logger.DebugFormat("GetReleaseExistStatus(): Selected release '{0}' for the name '{1}' based on the highest number of PURL IDs.\n", releasestatus.sw360Releases?.Name, name);
            releasestatus.isReleaseExist = !string.IsNullOrEmpty(releasestatus.sw360Releases?.ExternalIds?.Package_Url) || !string.IsNullOrEmpty(releasestatus.sw360Releases?.ExternalIds?.Purl_Id);

            return releasestatus;
        }

        /// <summary>
        /// gets component exist status
        /// </summary>
        /// <param name="name"></param>
        /// <param name="externlaIdKey"></param>
        /// <param name="sw360components"></param>
        /// <returns>comonent status</returns>
        private static ComponentStatus GetComponentExistStatus(string name, string externlaIdKey, IList<Sw360Components> sw360components)
        {
            Dictionary<int, Sw360Components> componentCollection = new Dictionary<int, Sw360Components>();

            Logger.DebugFormat("GetComponentExistStatus(): Identifying component exist status from SW360 : {0}", name);
            foreach (var componentsData in sw360components)
            {
                string packageUrl = string.Empty;
                packageUrl = GetPackageUrlValue(externlaIdKey, componentsData, packageUrl);
                Logger.DebugFormat("GetComponentExistStatus(): Component Name : {0} from {1}", name, packageUrl);
                try
                {
                    var purlids = JsonConvert.DeserializeObject<List<string>>(packageUrl);

                    if (componentCollection.TryGetValue(purlids.Count, out Sw360Components value) && value.Name.ToLower().Equals(name.ToLower()))
                    {
                        // Do nothing
                    }
                    else if (!componentCollection.TryAdd(purlids.Count, componentsData))
                    {
                        componentCollection[1] = componentsData;
                    }
                }
                catch (JsonReaderException)
                {
                    UpdateCollection(name, ref componentCollection, componentsData);
                }
            }
            ComponentStatus component = new ComponentStatus();

            component.Sw360components = componentCollection[componentCollection.Keys.Max()];
            Logger.DebugFormat("GetComponentExistStatus(): Component Name : {0} selected {1},based on the highest number of PURL IDs \n", name, component.Sw360components?.Name);
            component.isComponentExist = !string.IsNullOrEmpty(component.Sw360components?.ExternalIds?.Package_Url) || !string.IsNullOrEmpty(component.Sw360components?.ExternalIds?.Purl_Id);
            return component;

        }

        /// <summary>
        /// gets package url
        /// </summary>
        /// <param name="externlaIdKey"></param>
        /// <param name="release"></param>
        /// <param name="packageUrl"></param>
        /// <returns>value</returns>
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

        /// <summary>
        /// gets package url value
        /// </summary>
        /// <param name="externlaIdKey"></param>
        /// <param name="components"></param>
        /// <param name="packageUrl"></param>
        /// <returns>value</returns>
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

        /// <summary>
        /// update collections
        /// </summary>
        /// <param name="name"></param>
        /// <param name="componentCollection"></param>
        /// <param name="components"></param>
        private static void UpdateCollection(string name, ref Dictionary<int, Sw360Components> componentCollection, Sw360Components components)
        {
            if (componentCollection.TryGetValue(1, out Sw360Components value) && value.Name.ToLower().Equals(name.ToLower()))
            {
                // Do nothing
            }
            else if (!componentCollection.TryAdd(1, components))
            {
                componentCollection[1] = components;
            }
        }

        /// <summary>
        /// update collections
        /// </summary>
        /// <param name="name"></param>
        /// <param name="releaseCollection"></param>
        /// <param name="release"></param>
        private static void UpdateCollection(string name, ref Dictionary<int, Sw360Releases> releaseCollection, Sw360Releases release)
        {
            if (releaseCollection.TryGetValue(1, out Sw360Releases value) && value.Name.ToLower().Equals(name.ToLower()))
            {
                // Do nothing
            }
            else if (!releaseCollection.TryAdd(1, release))
            {
                releaseCollection[1] = release;
            }
        }

        #endregion
    }
}
