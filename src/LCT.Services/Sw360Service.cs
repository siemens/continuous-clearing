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

        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
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
        public async Task<List<Components>> GetDuplicateComponentsByPurlId(List<Components> listOfComponentsToBom)
        {
            return InvalidComponentsIdentifiedByPurlId;
        }

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
            if (InvalidComponentsIdentifiedByPurlId.Count != 0)
            {
                listOfComponentsToBom.RemoveAll(component =>
                    InvalidComponentsIdentifiedByPurlId.Any(invalid =>
                        invalid.Name?.Trim().Equals(component.Name?.Trim(), StringComparison.OrdinalIgnoreCase) == true &&
                        invalid.Version?.Trim().Equals(component.Version?.Trim(), StringComparison.OrdinalIgnoreCase) == true &&
                        invalid.ReleaseExternalId?.Equals(component.ReleaseExternalId) == true
                    ));
            }
            return availableComponentList;
        }

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

        private static Sw360Components FindMatchingComponent(IList<Sw360Components> sw360ComponentList, Components component)
        {
            return sw360ComponentList.FirstOrDefault(x =>
                x.Name?.Trim().ToLowerInvariant() == component?.Name?.Trim().ToLowerInvariant());
        }

        private static bool ValidateAndProcessComponent(Sw360Releases sw360Release, Sw360Components sw360Component, Components component)
        {
            // If no external IDs exist, add to available list
            if (string.IsNullOrEmpty(sw360Component?.ExternalIds?.Package_Url)
                && string.IsNullOrEmpty(sw360Component?.ExternalIds?.Purl_Id))
            {
                AddToAvailableList(sw360Release, component);
                return true;
            }

            // Log external IDs for debugging
            Logger.Debug($"GetAvailableComponenentsList(): Component Name - {component.Name}, Version - {component.Version} " +
                        $"validating Externalids list - {sw360Component.ExternalIds?.Package_Url},{sw360Component.ExternalIds?.Purl_Id}");

            // Validate project type and PURL
            if (!ValidateProjectTypePurl(sw360Component, component))
            {
                return false;
            }

            AddToAvailableList(sw360Release, component);
            return true;
        }

        private static bool ValidateProjectTypePurl(Sw360Components sw360Component, Components component)
        {
            if (string.IsNullOrEmpty(component?.ProjectType)
                || !Dataconstant.PurlCheck().TryGetValue(component.ProjectType.ToUpperInvariant(), out string projectPurlId))
            {
                return false;
            }

            Logger.Debug($"GetAvailableComponenentsList(): Validating with PURL ID: {projectPurlId}");

            bool hasMatchingExternalId = sw360Component.ExternalIds.Package_Url?.Contains(projectPurlId, StringComparison.OrdinalIgnoreCase) == true
                || sw360Component.ExternalIds.Purl_Id?.Contains(projectPurlId, StringComparison.OrdinalIgnoreCase) == true;

            if (!hasMatchingExternalId)
            {
                HandleMismatchedPurlId(sw360Component, component);
                return false;
            }

            Logger.Debug($"GetAvailableComponenentsList(): Component Name'{component.Name}' PURL check ID matched with SW360 component PURL ID");
            return true;
        }

        private static void HandleMismatchedPurlId(Sw360Components sw360Component, Components component)
        {
            Logger.Debug($"GetAvailableComponenentsList(): Component Name '{component.Name}' PURL ID mismatched with SW360 component PURL ID");
            Logger.Warn($"Component Name '{component.Name}' already exists in SW360 with different package type PURL ID. Skipping this component.");

            component.InValidComponentByPurlid = true;
            component.ComponentLink = sw360Component.Links?.Self?.Href;
            component.ComponentID = CommonHelper.GetSubstringOfLastOccurance(component.ComponentLink, "/");
            InvalidComponentsIdentifiedByPurlId.Add(component);
        }

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
