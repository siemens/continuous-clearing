// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.Common.Model;
using LCT.Facade.Interfaces;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace LCT.Facade
{

    /// <summary>
    ///SW3360 Api Communication Facade class
    /// </summary>
    public class SW360ApicommunicationFacade : ISW360ApicommunicationFacade
    {
        private readonly ISw360ApiCommunication m_sw360ApiCommunication;
        private readonly bool m_TestMode;

        public SW360ApicommunicationFacade(SW360ConnectionSettings sw360ConnectionSettings)
        {
            m_sw360ApiCommunication = new SW360Apicommunication(sw360ConnectionSettings);
            m_TestMode = sw360ConnectionSettings.IsTestMode;
        }

        public SW360ApicommunicationFacade(ISw360ApiCommunication sw360ApiCommunication, bool testMode)
        {
            m_sw360ApiCommunication = sw360ApiCommunication;
            m_TestMode = testMode;
        }

        public Task<string> GetProjects()
        {
            return m_sw360ApiCommunication.GetProjects();
        }

        public Task<string> GetSw360Users()
        {
            return m_sw360ApiCommunication.GetSw360Users();
        }

        public Task<string> GetProjectsByName(string projectName)
        {
            return m_sw360ApiCommunication.GetProjectsByName(projectName);
        }

        public Task<HttpResponseMessage> GetProjectsByTag(string projectTag)
        {
            return m_sw360ApiCommunication.GetProjectsByTag(projectTag);
        }

        public Task<HttpResponseMessage> GetProjectById(string projectId)
        {
            return m_sw360ApiCommunication.GetProjectById(projectId);
        }

        public Task<string> GetReleases()
        {
            return m_sw360ApiCommunication.GetReleases();
        }
        public Task<string> TriggerFossologyProcess(string releaseId, string sw360link)
        {
            return m_sw360ApiCommunication.TriggerFossologyProcess(releaseId, sw360link);
        }
        public Task<HttpResponseMessage> CheckFossologyProcessStatus(string link, string correlationId)
        {
            return m_sw360ApiCommunication.CheckFossologyProcessStatus(link,correlationId);
        }
        public Task<string> GetComponents(string correlationId)
        {
            return m_sw360ApiCommunication.GetComponents(correlationId);
        }

        public Task<HttpResponseMessage> GetReleaseById(string releaseId, string correlationId)
        {
            return m_sw360ApiCommunication.GetReleaseById(releaseId, correlationId);
        }

        public Task<HttpResponseMessage> GetReleaseByLink(string releaseLink)
        {
            return m_sw360ApiCommunication.GetReleaseByLink(releaseLink);
        }

        public async Task<HttpResponseMessage> LinkReleasesToProject(HttpContent httpContent, string sw360ProjectId,string correlationId)
        {
            return await m_sw360ApiCommunication.LinkReleasesToProject(httpContent, sw360ProjectId, correlationId);
        }

        public async Task<HttpResponseMessage> CreateComponent(CreateComponent createComponentContent, string correlationId)
        {
            return await m_sw360ApiCommunication.CreateComponent(createComponentContent, correlationId);
        }

        public async Task<HttpResponseMessage> CreateRelease(Releases createReleaseContent,string correlationId)
        {
            return await m_sw360ApiCommunication.CreateRelease(createReleaseContent, correlationId);
        }

        public Task<string> GetReleaseOfComponentById(string componentId, string correlationId)
        {
            return m_sw360ApiCommunication.GetReleaseOfComponentById(componentId, correlationId);
        }

        public Task<string> GetReleaseAttachments(string releaseAttachmentsUrl)
        {
            return m_sw360ApiCommunication.GetReleaseAttachments(releaseAttachmentsUrl);
        }

        public Task<string> GetAttachmentInfo(string attachmentUrl)
        {
            return m_sw360ApiCommunication.GetAttachmentInfo(attachmentUrl);
        }

        public void DownloadAttachmentUsingWebClient(string attachmentDownloadLink, string fileName)
        {
            m_sw360ApiCommunication.DownloadAttachmentUsingWebClient(attachmentDownloadLink, fileName);
        }

        public async Task<HttpResponseMessage> UpdateRelease(string releaseId, HttpContent httpContent,string correlationId)
        {
            return await m_sw360ApiCommunication.UpdateRelease(releaseId, httpContent, correlationId);
        }

        public async Task<HttpResponseMessage> UpdateComponent(string componentId, HttpContent httpContent)
        {
            return await m_sw360ApiCommunication.UpdateComponent(componentId, httpContent);
        }

        public string AttachComponentSourceToSW360(AttachReport attachReport)
        {
            return m_sw360ApiCommunication.AttachComponentSourceToSW360(attachReport);
        }

        public Task<string> GetReleaseByCompoenentName(string componentName, string correlationId)
        {
            return m_sw360ApiCommunication.GetReleaseByCompoenentName(componentName,correlationId);
        }

        public Task<HttpResponseMessage> GetComponentDetailsByUrl(string componentLink)
        {

            return m_sw360ApiCommunication.GetComponentDetailsByUrl(componentLink);
        }

        public Task<string> GetComponentByName(string componentName, string correlationId)
        {
            return m_sw360ApiCommunication.GetComponentByName(componentName,correlationId);
        }
        public Task<HttpResponseMessage> GetComponentUsingName(string componentName)
        {
            return m_sw360ApiCommunication.GetComponentUsingName(componentName);
        }

        public Task<HttpResponseMessage> UpdateLinkedRelease(string projectId, string releaseId, UpdateLinkedRelease updateLinkedRelease)
        {
            return m_sw360ApiCommunication.UpdateLinkedRelease(projectId, releaseId, updateLinkedRelease);
        }

        public Task<HttpResponseMessage> GetReleaseByExternalId(string purlId, string externalIdKey = "", string correlationId="")
        {
            return m_sw360ApiCommunication.GetReleaseByExternalId(purlId, externalIdKey, correlationId);
        }

        public Task<HttpResponseMessage> GetComponentByExternalId(string purlId, string externalIdKey = "", string correlationId = "")
        {
            return m_sw360ApiCommunication.GetComponentByExternalId(purlId, externalIdKey, correlationId);
        }
        public Task<HttpResponseMessage> GetAllReleasesWithAllData(int page, int pageEntries, string correlationId)
        {
            return m_sw360ApiCommunication.GetAllReleasesWithAllData(page, pageEntries, correlationId);
        }
    }
}
