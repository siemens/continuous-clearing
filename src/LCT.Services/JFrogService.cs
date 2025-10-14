// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
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
    /// The JfrogService Class
    /// </summary>
    public class JFrogService : IJFrogService
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly IJfrogAqlApiCommunicationFacade m_JFrogApiCommunicationFacade;

        public JFrogService(IJfrogAqlApiCommunicationFacade jFrogApiCommunicationFacade)
        {
            m_JFrogApiCommunicationFacade = jFrogApiCommunicationFacade;
        }
        private static async Task<IList<AqlResult>> GetComponentDataByRepo(Func<string, Task<HttpResponseMessage>> apiCall, string repoName)
        {
            HttpResponseMessage httpResponseMessage = null;
            IList<AqlResult> aqlResult = new List<AqlResult>();

            try
            {
                httpResponseMessage = await apiCall(repoName);
                if (httpResponseMessage == null || !httpResponseMessage.IsSuccessStatusCode)
                {
                    return new List<AqlResult>();
                }

                string stringData = httpResponseMessage.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                var aqlResponse = JsonConvert.DeserializeObject<AqlResponse>(stringData);
                aqlResult = aqlResponse?.Results ?? new List<AqlResult>();
            }
            catch (HttpRequestException httpException)
            {
                Logger.Debug(httpException);
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                Logger.Debug(invalidOperationExcep);
            }
            catch (TaskCanceledException taskCancelledException)
            {
                Logger.Debug(taskCancelledException);
            }

            return aqlResult;
        }

        public async Task<IList<AqlResult>> GetInternalComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(m_JFrogApiCommunicationFacade.GetInternalComponentDataByRepo, repoName);
        }

        public async Task<IList<AqlResult>> GetNpmComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(m_JFrogApiCommunicationFacade.GetNpmComponentDataByRepo, repoName);
        }

        public async Task<IList<AqlResult>> GetPypiComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(m_JFrogApiCommunicationFacade.GetPypiComponentDataByRepo, repoName);
        }

        public async Task<IList<AqlResult>> GetCargoComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(m_JFrogApiCommunicationFacade.GetCargoComponentDataByRepo, repoName);
        }
        
#nullable enable
        public async Task<AqlResult?> GetPackageInfo(ComponentsToArtifactory component)
        {
            HttpResponseMessage? httpResponseMessage = null;
            AqlResult? aqlResult = null;
            try
            {
                httpResponseMessage = await m_JFrogApiCommunicationFacade.GetPackageInfo(component);
                httpResponseMessage.EnsureSuccessStatusCode();

                string stringData = httpResponseMessage.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                var aqlResponse = JsonConvert.DeserializeObject<AqlResponse>(stringData);
                aqlResult = aqlResponse?.Results.FirstOrDefault();
            }
            catch (HttpRequestException httpException)
            {
                Logger.Debug(httpException);
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                Logger.Debug(invalidOperationExcep);
            }
            catch (TaskCanceledException taskCancelledException)
            {
                Logger.Debug(taskCancelledException);
            }

            return aqlResult;
        }

        public async Task<HttpResponseMessage> CheckJFrogConnectivity()
        {
            HttpResponseMessage? httpResponseMessage = null;
            httpResponseMessage = await m_JFrogApiCommunicationFacade.CheckConnection();

            return httpResponseMessage;
        }
    }
}
