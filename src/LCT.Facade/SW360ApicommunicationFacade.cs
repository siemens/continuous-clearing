// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.Common.Model;
using LCT.Facade.Interfaces;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

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
        public Task<string> TriggerFossologyProcess(string releaseId,string sw360link)
        {
            return m_sw360ApiCommunication.TriggerFossologyProcess(releaseId, sw360link);
        }
        public Task<HttpResponseMessage> CheckFossologyProcessStatus(string link)
        {
            return m_sw360ApiCommunication.CheckFossologyProcessStatus(link);
        }
        public Task<string> GetComponents()
        {
            return m_sw360ApiCommunication.GetComponents();
        }

        public Task<HttpResponseMessage> GetReleaseById(string releaseId)
        {
            return m_sw360ApiCommunication.GetReleaseById(releaseId);
        }

        public Task<HttpResponseMessage> GetReleaseByLink(string releaseLink)
        {
            return m_sw360ApiCommunication.GetReleaseByLink(releaseLink);
        }

        public async Task<HttpResponseMessage> LinkReleasesToProject(HttpContent httpContent, string sw360ProjectId)
        {
            if (m_TestMode)
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return await m_sw360ApiCommunication.LinkReleasesToProject(httpContent, sw360ProjectId);
        }

        public async Task<HttpResponseMessage> CreateComponent(CreateComponent createComponentContent)
        {
            if (m_TestMode)
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return await m_sw360ApiCommunication.CreateComponent(createComponentContent);
        }

        public async Task<HttpResponseMessage> CreateRelease(Releases createReleaseContent)
        {
            if (m_TestMode)
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return await m_sw360ApiCommunication.CreateRelease(createReleaseContent);
        }

        public Task<string> GetReleaseOfComponentById(string componentId)
        {
            return m_sw360ApiCommunication.GetReleaseOfComponentById(componentId);
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

        public async Task<HttpResponseMessage> UpdateRelease(string releaseId, HttpContent httpContent)
        {
            if (m_TestMode)
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            return await m_sw360ApiCommunication.UpdateRelease(releaseId, httpContent);
        }

        public async Task<HttpResponseMessage> UpdateComponent(string componentId, HttpContent httpContent)
        {
            if (m_TestMode)
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            return await m_sw360ApiCommunication.UpdateComponent(componentId, httpContent);
        }

        public string AttachComponentSourceToSW360(AttachReport attachReport)
        {
            return m_sw360ApiCommunication.AttachComponentSourceToSW360(attachReport);
        }

        public Task<string> GetReleaseByCompoenentName(string componentName)
        {
            return m_sw360ApiCommunication.GetReleaseByCompoenentName(componentName);
        }

        public Task<HttpResponseMessage> GetComponentDetailsByUrl(string componentLink)
        {

            return m_sw360ApiCommunication.GetComponentDetailsByUrl(componentLink);
        }

        public Task<string> GetComponentByName(string componentName)
        {
            return m_sw360ApiCommunication.GetComponentByName(componentName);
        }
        public Task<HttpResponseMessage> GetComponentUsingName(string componentName)
        {
            return m_sw360ApiCommunication.GetComponentUsingName(componentName);
        }

        public Task<HttpResponseMessage> UpdateLinkedRelease(string projectId, string releaseId, UpdateLinkedRelease updateLinkedRelease)
        {
            return m_sw360ApiCommunication.UpdateLinkedRelease(projectId, releaseId, updateLinkedRelease);
        }

        public Task<HttpResponseMessage> GetReleaseByExternalId(string purlId, string externalIdKey = "")
        {
            return m_sw360ApiCommunication.GetReleaseByExternalId(purlId, externalIdKey);
        }

        public Task<HttpResponseMessage> GetComponentByExternalId(string purlId, string externalIdKey = "")
        {
            return m_sw360ApiCommunication.GetComponentByExternalId(purlId, externalIdKey);
        }
    }
}
