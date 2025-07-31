// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Facade.Interfaces;
using LCT.Services.Interface;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.Services
{
    /// <summary>
    /// The Sw360ProjectService class
    /// </summary>
    public class Sw360ProjectService : ISw360ProjectService
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly ISW360ApicommunicationFacade m_SW360ApiCommunicationFacade;


        public Sw360ProjectService(ISW360ApicommunicationFacade sw360ApiCommunicationFacede)
        {
            m_SW360ApiCommunicationFacade = sw360ApiCommunicationFacede;
        }

        /// <summary>
        /// Gets the ProjectName By ProjectID From SW360
        /// </summary>
        /// <param name="projectId">projectId</param>
        /// <param name="projectName">projectName</param>
        /// <returns>string</returns>
        public async Task<string> GetProjectNameByProjectIDFromSW360(string projectId, string projectName, ProjectReleases projectReleases)
        {
            string sw360ProjectName = string.Empty;

            try
            {
                var response = await m_SW360ApiCommunicationFacade.GetProjectById(projectId);
                if (response == null || !response.IsSuccessStatusCode)
                {
                    return string.Empty;
                }

                string result = response.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                if (!string.IsNullOrEmpty(result))
                {
                    var projectInfo = JsonConvert.DeserializeObject<ProjectReleases>(result);
                    sw360ProjectName = projectInfo?.Name;
                    projectReleases.Name = projectInfo?.Name;
                    projectReleases.Version = projectInfo?.Version;
                    projectReleases.State = projectInfo?.State;
                    projectReleases.ClearingState = projectInfo?.ClearingState;
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"Failed to connect SW360 : {ex.Message}");
                LogHandlingHelper.ExceptionErrorHandling("HTTP request to SW360 failed", "GetProjectNameByProjectIDFromSW360()", ex, $"Project ID: {projectId}");
                Environment.ExitCode = -1;
            }
            catch (AggregateException ex)
            {
                Logger.Error($"Failed to connect SW360 : {ex.Message}");
                LogHandlingHelper.ExceptionErrorHandling("An aggregate exception occurred while connecting to SW360", "GetProjectNameByProjectIDFromSW360()", ex, $"Project ID: {projectId}");
                Environment.ExitCode = -1;
            }

            return sw360ProjectName;
        }

        public async Task<List<ReleaseLinked>> GetAlreadyLinkedReleasesByProjectId(string projectId)
        {
            List<ReleaseLinked> alreadyLinkedReleases = new List<ReleaseLinked>();
            try
            {
                HttpResponseMessage projectResponsebyId = await m_SW360ApiCommunicationFacade.GetProjectById(projectId);
                if (projectResponsebyId.StatusCode != HttpStatusCode.OK)
                {
                    Logger.Debug($"GetProjectReleasesByProjectIdFromSw360()-{projectResponsebyId.StatusCode}:{projectResponsebyId.ReasonPhrase}");
                    return alreadyLinkedReleases;
                }

                Logger.Debug($"GetProjectReleasesByProjectIdFromSw360():Success StatusCode:{projectResponsebyId.StatusCode} " +
              $"& ReasonPhrase :{projectResponsebyId.ReasonPhrase}");

                string result = projectResponsebyId.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                var projectReleases = JsonConvert.DeserializeObject<ProjectReleases>(result);
                var sw360LinkedReleases = projectReleases?.LinkedReleases ?? new List<Sw360LinkedRelease>();
                foreach (Sw360LinkedRelease sw360Release in sw360LinkedReleases)
                {
                    string releaseUrl = sw360Release.Release ?? string.Empty;
                    ReleaseLinked releaseLinked = new ReleaseLinked
                    {
                        Comment = sw360Release.Comment,
                        ReleaseId = CommonHelper.GetSubstringOfLastOccurance(releaseUrl, "/"),
                        Relation = sw360Release.Relation
                    };
                    alreadyLinkedReleases.Add(releaseLinked);
                }
            }
            catch (HttpRequestException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get Already Linked Releases By ProjectId", $"MethodName:GetAlreadyLinkedReleasesByProjectId()", ex, "");
                Logger.Error($"GetAlreadyLinkedReleasesByProjectId()", ex);
            }
            catch (AggregateException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get Already Linked Releases By ProjectId", $"MethodName:GetAlreadyLinkedReleasesByProjectId()", ex, "");
                Logger.Error($"GetAlreadyLinkedReleasesByProjectId()", ex);
            }

            return alreadyLinkedReleases;
        }
    }
}
