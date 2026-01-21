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
using LCT.Common.Model;
using LCT.Facade.Interfaces;
using LCT.Services.Interface;
using log4net;
using log4net.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using File = System.IO.File;

namespace LCT.Services
{
    /// <summary>
    /// sw360 services class
    /// </summary>
    public class Sw360Service : ISW360Service
    {
        public static Stopwatch Sw360ServiceStopWatch { get; set; }
        private readonly IEnvironmentHelper environmentHelper;

        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ISW360ApicommunicationFacade m_SW360ApiCommunicationFacade;
        private readonly ISW360CommonService m_SW360CommonService;
        private static List<Components> availableComponentList = new List<Components>();
        private static readonly List<Components> InvalidComponentsIdentifiedByPurlId = new List<Components>();
        public Sw360Service(ISW360ApicommunicationFacade sw360ApiCommunicationFacade, IEnvironmentHelper _environmentHelper)
        {
            m_SW360ApiCommunicationFacade = sw360ApiCommunicationFacade;
            environmentHelper = _environmentHelper;
        }

        public Sw360Service(ISW360ApicommunicationFacade sw360ApiCommunicationFacade, ISW360CommonService sw360CommonService, IEnvironmentHelper _environmentHelper)
        {
            m_SW360ApiCommunicationFacade = sw360ApiCommunicationFacade;
            m_SW360CommonService = sw360CommonService;
            environmentHelper = _environmentHelper;
        }

        /// <summary>
        /// Retrieves a list of components that have duplicate Package URL (PURL) identifiers.
        /// </summary>
        /// <returns>A list of <see cref="Components"/> objects that share the same PURL identifier. The list is empty if no
        /// duplicates are found.</returns>
        public List<Components> GetDuplicateComponentsByPurlId()
        {
            return InvalidComponentsIdentifiedByPurlId;
        }

        /// <summary>
        /// Retrieves the list of components from SW360 that are available as releases and match the specified
        /// components.
        /// </summary>       
        /// <param name="listOfComponentsToBom">The list of components to check for available releases in SW360. Each component in the list is compared
        /// against the releases retrieved from SW360.</param>
        /// <returns>A list of components representing the available releases in SW360 that correspond to the specified
        /// components. The list is empty if no matching releases are found.</returns>
        public async Task<List<Components>> GetAvailableReleasesInSw360(List<Components> listOfComponentsToBom)
        {
            List<Components> availableComponentsList = new List<Components>();
            Sw360ServiceStopWatch = new Stopwatch();
            try
            {
                Sw360ServiceStopWatch.Start();
                string responseBody = await m_SW360ApiCommunicationFacade.GetReleases();
                Sw360ServiceStopWatch.Stop();
                Logger.Debug($"GetAvailableReleasesInSw360():Time taken to in GetReleases() call" +
                    $"-{TimeSpan.FromMilliseconds(Sw360ServiceStopWatch.ElapsedMilliseconds).TotalSeconds}");
                var modelMappedObject = JsonConvert.DeserializeObject<ComponentsRelease>(responseBody);

                if (modelMappedObject != null && modelMappedObject.Embedded?.Sw360Releases?.Count > 0)
                {
                    availableComponentsList = await GetAvailableComponenentsList(modelMappedObject.Embedded?.Sw360Releases, listOfComponentsToBom);
                }
                else
                {
                    Logger.Debug("GetAvailableReleasesInSw360() : Releases list found empty from the SW360 Server !!");
                    Logger.Error("SW360 server is not accessible while getting All Releases,Please wait for sometime and re run the pipeline again");
                    environmentHelper.CallEnvironmentExit(-1);
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.Debug($"GetAvailableReleasesInSw360():", ex);
                Logger.Error("SW360 server is not accessible,Please wait for sometime and re run the pipeline again");
                environmentHelper.CallEnvironmentExit(-1);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Debug($"GetAvailableReleasesInSw360():", ex);
                Logger.Error("SW360 server is not accessible,Please wait for sometime and re run the pipeline again");
                environmentHelper.CallEnvironmentExit(-1);
            }

            return availableComponentsList;
        }

        /// <summary>
        /// Retrieves release information for a specified component using the provided release link.
        /// </summary>
        /// <param name="releaseLink">The URL or identifier used to locate and retrieve the release data for the component. Cannot be null or
        /// empty.</param>
        /// <returns>A <see cref="ReleasesInfo"/> object containing the release details for the specified component. Returns an
        /// empty <see cref="ReleasesInfo"/> instance if no data is found or an error occurs.</returns>
        public async Task<ReleasesInfo> GetReleaseDataOfComponent(string releaseLink)
        {
            ReleasesInfo releasesInfo = new ReleasesInfo();
            try
            {
                HttpResponseMessage responseData = await GetReleaseInfoByReleaseId(releaseLink);
                string response = responseData?.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                var releaseResponse = JsonConvert.DeserializeObject<ReleasesInfo>(response);
                if (releaseResponse != null)
                {
                    releasesInfo = releaseResponse;
                }

            }
            catch (AggregateException e)
            {
                Environment.ExitCode = -1;
                Logger.Error($"GetComponentsClearingStatus():", e);
            }

            return releasesInfo;
        }

        /// <summary>
        /// Retrieves release information from the SW360 API using the specified release link.
        /// </summary>       
        /// <param name="releaseLink">The URL or link identifying the release to retrieve. Must not be null or empty. The release ID is extracted
        /// from the last segment of this link.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the HTTP response message with
        /// the release information if the request is successful; otherwise, the response may be null if an error
        /// occurs.</returns>
        public async Task<HttpResponseMessage> GetReleaseInfoByReleaseId(string releaseLink)
        {
            HttpResponseMessage responseBody = null;
            string releaseId = CommonHelper.GetSubstringOfLastOccurance(releaseLink, "/");
            try
            {
                responseBody = await m_SW360ApiCommunicationFacade.GetReleaseById(releaseId);
            }
            catch (HttpRequestException ex)
            {
                Environment.ExitCode = -1;
                Logger.Error($"GetReleaseInfoByReleaseId():", ex);
            }

            return responseBody;
        }

        /// <summary>
        /// Retrieves the release identifier for a specified component and version.
        /// </summary>       
        /// <param name="componentName">The name of the component for which to retrieve the release identifier. Cannot be null or empty.</param>
        /// <param name="version">The version of the component for which to retrieve the release identifier. Cannot be null or empty.</param>
        /// <returns>A string containing the release identifier if found; otherwise, an empty string.</returns>
        public async Task<string> GetComponentReleaseID(string componentName, string version)
        {
            string releaseId = ""; string href = "";
            try
            {
                string response = await m_SW360ApiCommunicationFacade.GetReleaseByCompoenentName(componentName);
                var responseData = JsonConvert.DeserializeObject<ComponentsRelease>(response);
                for (int index = 0; index < responseData?.Embedded?.Sw360Releases?.Count; index++)
                {
                    if (responseData.Embedded.Sw360Releases[index].Name.Equals(componentName, StringComparison.InvariantCultureIgnoreCase)
                        && responseData.Embedded.Sw360Releases[index].Version.Equals(version, StringComparison.InvariantCultureIgnoreCase))
                    {
                        href = responseData.Embedded.Sw360Releases[index].Links.Self.Href;
                        break;
                    }
                }

                releaseId = CommonHelper.GetSubstringOfLastOccurance(href, "/");
            }
            catch (HttpRequestException e)
            {
                Logger.Error($"GetComponentReleaseID():", e);
            }

            return releaseId;
        }

        /// <summary>
        /// Retrieves the download link and related metadata for an attachment associated with a release, given the
        /// attachment URL.
        /// </summary>       
        /// <param name="releaseAttachmentUrl">The URL of the release attachment for which to obtain the download link. Cannot be null, empty, or
        /// whitespace.</param>
        /// <returns>A <see cref="Sw360AttachmentHash"/> object containing the download link and related information. If the
        /// attachment is not available or the URL is invalid, the returned object will indicate that the source is not
        /// available.</returns>
        public async Task<Sw360AttachmentHash> GetAttachmentDownloadLink(string releaseAttachmentUrl)
        {
            Sw360AttachmentHash attachmentHash = new Sw360AttachmentHash();
            try
            {
                if (string.IsNullOrWhiteSpace(releaseAttachmentUrl))
                {
                    return attachmentHash;
                }

                string releaseAttachmentResponse = await m_SW360ApiCommunicationFacade.GetReleaseAttachments(releaseAttachmentUrl);
                var releaseAttachmentResponseData = JsonConvert.DeserializeObject<ReleaseAttachments>(releaseAttachmentResponse);
                attachmentHash = GetReleaseSourceAttachmentLink(releaseAttachmentResponseData?.Embedded?.Sw360attachments);

                if (!string.IsNullOrEmpty(attachmentHash.AttachmentLink))
                {
                    string attachmentLinkResponse = await m_SW360ApiCommunicationFacade.GetAttachmentInfo(attachmentHash.AttachmentLink);
                    var attachmentLinkResponseData = JsonConvert.DeserializeObject<AttachmentLink>(attachmentLinkResponse);
                    attachmentHash.SourceDownloadUrl = attachmentLinkResponseData?.Links?.Sw360DownloadLink?.DownloadUrl ?? string.Empty;
                    attachmentHash.isAttachmentSourcenotAvailableInSw360 = false;
                }
                else
                {
                    attachmentHash.isAttachmentSourcenotAvailableInSw360 = true;
                }
            }
            catch (HttpRequestException e)
            {
                Logger.Debug($"GetAttachmentDownloadLink():", e);
            }
            catch (AggregateException e)
            {
                Logger.Debug($"GetAttachmentDownloadLink():", e);
            }

            return attachmentHash;
        }

        /// <summary>
        /// Retrieves the attachment link, hash code, and name for the first attachment of type "SOURCE" from the
        /// specified collection.
        /// </summary>
        /// <param name="sw360attachments">A list of SW360 attachments to search for a source attachment. Cannot be null.</param>
        /// <returns>A Sw360AttachmentHash containing the link, hash code, and name of the first source attachment found;
        /// otherwise, an empty Sw360AttachmentHash if no source attachment exists.</returns>
        private static Sw360AttachmentHash GetReleaseSourceAttachmentLink(IList<Sw360Attachments> sw360attachments)
        {
            Sw360AttachmentHash attachmentHash = new Sw360AttachmentHash();
            for (int index = 0; index < sw360attachments?.Count; index++)
            {
                if (sw360attachments[index].AttachmentType.Equals("SOURCE", StringComparison.InvariantCultureIgnoreCase))
                {
                    attachmentHash.AttachmentLink = sw360attachments[index]?.Links?.Self?.Href ?? string.Empty;
                    attachmentHash.HashCode = sw360attachments[index]?.Sha1 ?? string.Empty;
                    attachmentHash.SW360AttachmentName = sw360attachments[index]?.Filename;
                }
            }
            return attachmentHash;
        }

        /// <summary>
        /// Downloads the source code attachment for a release from the specified SW360 attachment information.
        /// </summary>        
        /// <param name="fileName">The name of the component file to use as a base for the downloaded file path.</param>
        /// <param name="version">The version of the component associated with the source code to download.</param>
        /// <param name="attachmentHash">The SW360 attachment hash containing metadata and the download URL for the source code attachment. Cannot be
        /// null.</param>
        /// <returns>The full file path to the downloaded source code file if the download succeeds; otherwise, returns the
        /// original component file name if the download fails or the source download URL is not available.</returns>
        [ExcludeFromCodeCoverage]
        public string DownloadReleaseSourceCode(string fileName, string version, Sw360AttachmentHash attachmentHash)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string componentName = fileName;

            if (string.IsNullOrEmpty(attachmentHash.SourceDownloadUrl))
            {
                Logger.Debug($"DownloadReleaseSourceCode():Fossology Upload unsuccessful, Component source is not Found for {fileName}-{version} under sw360 attachments");
                Logger.Warn($"Fossology Upload unsuccessful, Component source is not Found for {fileName}-{version} under sw360 attachments");
                return componentName;
            }
            try
            {
                string filePath = $"{Path.GetTempPath()}ClearingTool\\DownloadedFiles/{attachmentHash.SW360AttachmentName}";
                fileName = $"{filePath}";
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                m_SW360ApiCommunicationFacade.DownloadAttachmentUsingWebClient(attachmentHash.SourceDownloadUrl, filePath);
            }
            catch (IOException ex)
            {
                Logger.Debug($"DownloadReleaseSourceCode:", ex);
                Logger.Warn($"Component download failed :{componentName}");

                return componentName;
            }
            catch (WebException e)
            {
                Logger.Debug($"DownloadReleaseSourceCode:", e);
                Logger.Warn($"Component download failed :{componentName}");

                return componentName;
            }

            if (fileName == componentName)
            {
                Logger.Logger.Log(null, Level.Debug, $"Component download failed: {componentName} ", null);
            }
            return fileName;
        }

        /// <summary>
        /// gets upload description frim sw360
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componetVersion"></param>
        /// <param name="sw360url"></param>
        /// <returns>description</returns>
        [ExcludeFromCodeCoverage]
        public async Task<string> GetUploadDescriptionfromSW360(string componentName, string componetVersion, string sw360url)
        {
            try
            {
                string href;
                string releaseurl;
                string releaseid = string.Empty;
                var response = await m_SW360ApiCommunicationFacade.GetReleaseByCompoenentName(componentName);
                var responseData = JsonConvert.DeserializeObject<ComponentsRelease>(response);

                for (int index = 0; index < responseData?.Embedded?.Sw360Releases?.Count; index++)
                {
                    if (responseData.Embedded.Sw360Releases[index].Name.Equals(componentName?.ToUpperInvariant()
, StringComparison.InvariantCultureIgnoreCase)
                        && responseData.Embedded.Sw360Releases[index].Version.Equals(componetVersion, StringComparison.InvariantCultureIgnoreCase))
                    {
                        href = responseData.Embedded.Sw360Releases[index].Links.Self.Href;
                        releaseid = CommonHelper.GetSubstringOfLastOccurance(href, "/");
                        releaseurl = sw360url + ApiConstant.Sw360ReleaseUrlApiSuffix + releaseid + "#/tab-Summary";
                        Logger.Debug($"GetUploadDescriptionfromSW360:ComponentName-{componentName}:{componetVersion} : {href}");
                        return releaseurl;
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Logger.Error($"GetUploadDescriptionfromSW360():", e);
            }
            return "";
        }

        /// <summary>
        /// gets available components list
        /// </summary>
        /// <param name="sw360Releases"></param>
        /// <param name="listOfComponentsToBom"></param>
        /// <returns>list of components</returns>
        private async Task<List<Components>> GetAvailableComponenentsList(IList<Sw360Releases> sw360Releases, List<Components> listOfComponentsToBom)
        {

            IList<Sw360Components> sw360ComponentList = await GetAvailableComponenentsListFromSw360();
            if (sw360Releases == null || sw360Releases.Count == 0)
            {
                return availableComponentList;
            }

            foreach (Components component in listOfComponentsToBom)
            {
                if (await CheckReleaseExistenceByExternalId(component) ||
                       CheckAvailabilityByNameAndVersion(sw360Releases, component, sw360ComponentList))
                {
                    Logger.Debug($"GetAvailableComponenentsList():  Release Exist : Release name - {component.Name}, version - {component.Version}");
                }
                else if (await CheckComponentExistenceByExternalId(component) ||
                         CheckAvailabilityByName(sw360ComponentList, component))
                {
                    Logger.Debug($"GetAvailableComponenentsList():  Component Exist : Release name - {component.Name}, version - {component.Version}");
                }
                else
                {
                    // Do Nothing or to be implemented
                }
            }
            RemoveInvalidComponentsByPurlId(listOfComponentsToBom);
            return availableComponentList;
        }

        /// <summary>
        /// remove invalid component by purl id
        /// </summary>
        /// <param name="components"></param>
        private static void RemoveInvalidComponentsByPurlId(List<Components> components)
        {
            if (InvalidComponentsIdentifiedByPurlId.Count == 0)
                return;

            components.RemoveAll(component =>
                InvalidComponentsIdentifiedByPurlId.Any(invalid =>
                    invalid.Name?.Trim().Equals(component.Name?.Trim(), StringComparison.OrdinalIgnoreCase) == true &&
                    invalid.Version?.Trim().Equals(component.Version?.Trim(), StringComparison.OrdinalIgnoreCase) == true &&
                    invalid.ReleaseExternalId?.Equals(component.ReleaseExternalId) == true
                ));
        }

        /// <summary>
        /// check availability by name and version
        /// </summary>
        /// <param name="sw360Releases"></param>
        /// <param name="component"></param>
        /// <param name="sw360ComponentList"></param>
        /// <returns>boolean value</returns>
        private static bool CheckAvailabilityByNameAndVersion(IList<Sw360Releases> sw360Releases, Components component, IList<Sw360Components> sw360ComponentList)
        {
            Logger.Debug($"CheckAvailabilityByNameAndVersion(): Starting check for component '{component?.Name}' version '{component?.Version}'");

            var sw360Release = FindMatchingRelease(sw360Releases, component);
            if (sw360Release == null)
            {
                return false;
            }

            var sw360Component = FindMatchingComponent(sw360ComponentList, component);
            if (sw360Component == null)
            {
                return false;
            }

            return ValidateAndProcessComponent(sw360Release, sw360Component, component);
        }

        /// <summary>
        /// fins matching release
        /// </summary>
        /// <param name="sw360Releases"></param>
        /// <param name="component"></param>
        /// <returns>releases data</returns>
        private static Sw360Releases FindMatchingRelease(IList<Sw360Releases> sw360Releases, Components component)
        {
            // Find regular version match
            var sw360Release = sw360Releases.FirstOrDefault(x =>
                x.Name?.Trim().ToLowerInvariant() == component?.Name?.Trim().ToLowerInvariant()
                && x.Version?.Trim().ToLowerInvariant() == component?.Version?.Trim().ToLowerInvariant());

            // Check for Debian specific version if needed
            if (sw360Release == null && component.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["DEBIAN"]))
            {
                string debianVersion = $"{component?.Version?.Trim().ToLowerInvariant() ?? string.Empty}.debian";
                sw360Release = sw360Releases.FirstOrDefault(x =>
                    x.Name?.Trim().ToLowerInvariant() == component?.Name?.Trim().ToLowerInvariant()
                    && x.Version?.Trim().ToLowerInvariant() == debianVersion);
            }

            return sw360Release;
        }

        /// <summary>
        /// finds matching component
        /// </summary>
        /// <param name="sw360ComponentList"></param>
        /// <param name="component"></param>
        /// <returns>component</returns>
        private static Sw360Components FindMatchingComponent(IList<Sw360Components> sw360ComponentList, Components component)
        {
            return sw360ComponentList.FirstOrDefault(x =>
                x.Name?.Trim().ToLowerInvariant() == component?.Name?.Trim().ToLowerInvariant());
        }

        /// <summary>
        /// validate and process component
        /// </summary>
        /// <param name="sw360Release"></param>
        /// <param name="sw360Component"></param>
        /// <param name="component"></param>
        /// <returns>boolean value</returns>
        private static bool ValidateAndProcessComponent(Sw360Releases sw360Release, Sw360Components sw360Component, Components component)
        {
            if (string.IsNullOrEmpty(sw360Component?.ExternalIds?.Package_Url)
                && string.IsNullOrEmpty(sw360Component?.ExternalIds?.Purl_Id))
            {
                AddToAvailableList(sw360Release, component);
                return true;
            }

            Logger.Debug($"GetAvailableComponenentsList(): Component Name - {component.Name}, Version - {component.Version} " +
                        $"validating Externalids list - {sw360Component.ExternalIds?.Package_Url},{sw360Component.ExternalIds?.Purl_Id}");

            if (!ValidateProjectTypePurl(sw360Component, component))
            {
                return false;
            }

            AddToAvailableList(sw360Release, component);
            return true;
        }

        /// <summary>
        /// validates project type url
        /// </summary>
        /// <param name="sw360Component"></param>
        /// <param name="component"></param>
        /// <returns>boolean value</returns>
        private static bool ValidateProjectTypePurl(Sw360Components sw360Component, Components component)
        {
            if (string.IsNullOrEmpty(component?.ProjectType)
                || !Dataconstant.PurlCheck().TryGetValue(component.ProjectType.ToUpperInvariant(), out string projectPurlCheckId))
            {
                return false;
            }

            Logger.Debug($"GetAvailableComponenentsList(): Validating with PURL ID: {projectPurlCheckId}");

            bool hasMatchingExternalId = sw360Component.ExternalIds.Package_Url?.Contains(projectPurlCheckId, StringComparison.OrdinalIgnoreCase) == true
                || sw360Component.ExternalIds.Purl_Id?.Contains(projectPurlCheckId, StringComparison.OrdinalIgnoreCase) == true;

            if (!hasMatchingExternalId)
            {
                HandleMismatchedPurlId(sw360Component, component);
                return false;
            }

            Logger.Debug($"GetAvailableComponenentsList(): Component Name'{component.Name}' PURL check ID matched with SW360 component PURL ID");
            return true;
        }

        /// <summary>
        /// handle mismatched url id
        /// </summary>
        /// <param name="sw360Component"></param>
        /// <param name="component"></param>
        private static void HandleMismatchedPurlId(Sw360Components sw360Component, Components component)
        {
            Logger.Debug($"GetAvailableComponenentsList(): Component Name '{component.Name}' PURL ID mismatched with SW360 component PURL ID");
            Logger.Warn($"Component Name '{component.Name}' already exists in SW360 with different package type PURL ID. Skipping this component.");

            component.InvalidComponentByPurlId = true;
            component.ComponentLink = sw360Component.Links?.Self?.Href;
            component.ComponentId = CommonHelper.GetSubstringOfLastOccurance(component.ComponentLink, "/");
            InvalidComponentsIdentifiedByPurlId.Add(component);
        }

        /// <summary>
        /// adds to available list
        /// </summary>
        /// <param name="sw360Release"></param>
        /// <param name="component"></param>
        private static void AddToAvailableList(Sw360Releases sw360Release, Components component)
        {
            availableComponentList.Add(new Components
            {
                Name = sw360Release.Name,
                Version = sw360Release.Version,
                ReleaseLink = sw360Release.Links?.Self?.Href,
                ReleaseExternalId = component.ReleaseExternalId,
                ComponentExternalId = component.ComponentExternalId
            });
        }

        /// <summary>
        /// gets available component list from sw360
        /// </summary>
        /// <returns></returns>
        private async Task<IList<Sw360Components>> GetAvailableComponenentsListFromSw360()
        {
            IList<Sw360Components> componentsList = new List<Sw360Components>();
            try
            {
                string responseBody = await m_SW360ApiCommunicationFacade.GetComponents();
                var componentsDataModel = JsonConvert.DeserializeObject<ComponentsModel>(responseBody);
                componentsList = componentsDataModel?.Embedded?.Sw360components;
            }
            catch (HttpRequestException ex)
            {
                Environment.ExitCode = -1;
                Logger.Error($"GetAvailableComponenentsListFromSw360():", ex);
            }
            return componentsList;
        }

        /// <summary>
        /// checks availability by name
        /// </summary>
        /// <param name="sw360Components"></param>
        /// <param name="component"></param>
        /// <returns>boolean value</returns>
        private static bool CheckAvailabilityByName(IList<Sw360Components> sw360Components, Components component)
        {
            //checking for component existance with name 
            bool isComponentAvailable = false;
            Sw360Components sw360Component =
                sw360Components.FirstOrDefault(x => x.Name?.Trim().ToLowerInvariant() == component?.Name?.Trim().ToLowerInvariant());
            if (sw360Component != null)
            {
                availableComponentList.Add(new Components()
                {
                    Name = sw360Component.Name,
                    Version = string.Empty,
                    ReleaseLink = string.Empty,
                    ComponentExternalId = component.ComponentExternalId,
                    ReleaseExternalId = string.Empty
                });
                isComponentAvailable = true;
            }
            return isComponentAvailable;
        }

        /// <summary>
        /// checks component existence by external id
        /// </summary>
        /// <param name="componentToBomData"></param>
        /// <returns>boolean value</returns>
        private async Task<bool> CheckComponentExistenceByExternalId(Components componentToBomData)
        {
            Logger.Debug($"CheckComponentExistenceByExternalId(): Component - {componentToBomData.Name}");
            ComponentStatus componentstatus = new ComponentStatus();

            try
            {
                componentstatus = await m_SW360CommonService.GetComponentDataByExternalId(componentToBomData.Name, componentToBomData.ComponentExternalId);

                if (componentstatus.isComponentExist)
                {
                    availableComponentList.Add(new Components()
                    {
                        Name = componentToBomData.Name,
                        Version = string.Empty,
                        ReleaseLink = string.Empty,
                        ComponentExternalId = componentToBomData.ComponentExternalId,
                        ReleaseExternalId = string.Empty
                    });
                }
            }
            catch (HttpRequestException ex)
            {
                componentstatus.isComponentExist = false;
                Logger.Error($"CheckComponentExistenceByExternalId():", ex);
            }
            catch (AggregateException ex)
            {
                componentstatus.isComponentExist = false;
                Logger.Error($"CheckComponentExistenceByExternalId():", ex);
            }

            return componentstatus.isComponentExist;
        }

        /// <summary>
        /// checks release existence by external id
        /// </summary>
        /// <param name="componentToBomData"></param>
        /// <returns>boolean value</returns>
        private async Task<bool> CheckReleaseExistenceByExternalId(Components componentToBomData)
        {
            Logger.Debug($"CheckReleaseExistenceByExternalId():  start : Release name - {componentToBomData.Name}, version - {componentToBomData.Version}");
            Releasestatus releaseStatus = new Releasestatus();

            try
            {
                releaseStatus = await m_SW360CommonService.GetReleaseDataByExternalId(componentToBomData.Name, componentToBomData.Version, componentToBomData.ReleaseExternalId);
                if (releaseStatus.isReleaseExist)
                {
                    availableComponentList.Add(new Components()
                    {
                        Name = componentToBomData.Name,
                        Version = componentToBomData.Version,
                        ReleaseLink = releaseStatus.sw360Releases.Links?.Self?.Href,
                        ReleaseExternalId = componentToBomData.ReleaseExternalId,
                        ComponentExternalId = componentToBomData.ComponentExternalId
                    });
                }
            }
            catch (HttpRequestException ex)
            {
                releaseStatus.isReleaseExist = false;
                Logger.Error($"CheckReleaseExistenceByExternalId():", ex);
            }
            catch (AggregateException ex)
            {
                releaseStatus.isReleaseExist = false;
                Logger.Error($"CheckReleaseExistenceByExternalId():", ex);
            }

            return releaseStatus.isReleaseExist;
        }
    }
}
