// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.Foss;
using LCT.Common;
using LCT.Common.Constants;
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
using System.Text;
using System.Threading.Tasks;

namespace LCT.Services
{
    /// <summary>
    /// sw360 services class
    /// </summary>
    public class Sw360Service : ISW360Service
    {
        public static Stopwatch Sw360ServiceStopWatch { get; set; }

        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ISW360ApicommunicationFacade m_SW360ApiCommunicationFacade;
        private readonly ISW360CommonService m_SW360CommonService;
        private static List<Components> availableComponentList = new List<Components>();
        public Sw360Service(ISW360ApicommunicationFacade sw360ApiCommunicationFacade)
        {
            m_SW360ApiCommunicationFacade = sw360ApiCommunicationFacade;
        }

        public Sw360Service(ISW360ApicommunicationFacade sw360ApiCommunicationFacade, ISW360CommonService sw360CommonService)
        {
            m_SW360ApiCommunicationFacade = sw360ApiCommunicationFacade;
            m_SW360CommonService = sw360CommonService;
        }

        public async Task<List<Components>> GetAvailableReleasesInSw360(List<Components> listOfComponentsToBom)
        {
            List<Components> availableComponentsList = new List<Components>();
            Sw360ServiceStopWatch = new Stopwatch();
            try
            {
                Sw360ServiceStopWatch.Start();
                string responseBody = await m_SW360ApiCommunicationFacade.GetReleases();
                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    Logger.Warn("Available release list found empty from the SW360 Server!");
                }

                Sw360ServiceStopWatch.Stop();
                Logger.Debug($"GetAvailableReleasesInSw360():Time taken to in GetReleases() call" +
                    $"-{TimeSpan.FromMilliseconds(Sw360ServiceStopWatch.ElapsedMilliseconds).TotalSeconds}");

                var modelMappedObject = JsonConvert.DeserializeObject<ComponentsRelease>(responseBody);

                if (modelMappedObject != null)
                {
                    availableComponentsList = await GetAvailableComponenentsList(modelMappedObject.Embedded?.Sw360Releases, listOfComponentsToBom);
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.Debug($"GetAvailableReleasesInSw360():", ex);
                Logger.Error("SW360 server is not accessible,Please wait for sometime and re run the pipeline again");
                Environment.Exit(-1);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Debug($"GetAvailableReleasesInSw360():", ex);
                Logger.Error("SW360 server is not accessible,Please wait for sometime and re run the pipeline again");
                Environment.Exit(-1);
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
                    if (responseData.Embedded.Sw360Releases[index].Name.ToUpperInvariant() == componentName.ToUpperInvariant()
                        && responseData.Embedded.Sw360Releases[index].Version.ToUpperInvariant() == version.ToLowerInvariant())
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
                if (sw360attachments[index].AttachmentType.ToUpperInvariant() == "SOURCE")
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
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

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
                    if (responseData.Embedded.Sw360Releases[index].Name.ToUpperInvariant() == componentName?.ToUpperInvariant()
                        && responseData.Embedded.Sw360Releases[index].Version.ToLowerInvariant() == componetVersion.ToLowerInvariant())
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
                       CheckAvailabilityByNameAndVersion(sw360Releases, component))
                {
                    Logger.Debug($"GetAvailableComponenentsList():  Release Exist : Release name - {component.Name}, version - {component.Version}");
                }
                else if (await CheckComponentExistenceByExternalId(component) ||
                         CheckAvailabilityByName(sw360ComponentList, component))
                {
                    Logger.Debug($"GetAvailableComponenentsList():  Compoennt Exist : Release name - {component.Name}, version - {component.Version}");
                }
                else
                {
                    // Do Nothing or to be implemented
                }
            }

            return availableComponentList;
        }

        private static bool CheckAvailabilityByNameAndVersion(IList<Sw360Releases> sw360Releases, Components component)
        {
            //checking for release existance with name and version
            bool isReleaseAvailable = false;
            Sw360Releases sw360Release =
                sw360Releases.FirstOrDefault(x => x.Name?.Trim().ToLowerInvariant() == component?.Name?.Trim().ToLowerInvariant()
                && x.Version?.Trim().ToLowerInvariant() == component?.Version?.Trim().ToLowerInvariant());

            if (sw360Release == null && component.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["DEBIAN"]))
            {
                string debianVersion = $"{component?.Version?.Trim().ToLowerInvariant() ?? string.Empty}.debian";
                sw360Release = sw360Releases.FirstOrDefault(x => x.Name?.Trim().ToLowerInvariant() == component?.Name?.Trim().ToLowerInvariant()
                && x.Version?.Trim().ToLowerInvariant() == debianVersion);
            }

            if (sw360Release != null)
            {
                availableComponentList.Add(new Components()
                {
                    Name = sw360Release.Name,
                    Version = sw360Release.Version,
                    ReleaseLink = sw360Release.Links?.Self?.Href,
                    ReleaseExternalId = component.ReleaseExternalId,
                    ComponentExternalId = component.ComponentExternalId
                });
                isReleaseAvailable = true;
            }
            return isReleaseAvailable;
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
            Logger.Debug($"CheckReleaseExistenceByExternalId():  Get Release Data by useing ExternalId : Release name - {componentToBomData.Name}, version - {componentToBomData.Version}");
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
