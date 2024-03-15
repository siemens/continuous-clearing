// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Facade.Interfaces;
using LCT.Services.Interface;
using System.Net.Http;
using System.Threading.Tasks;
using log4net;
using System.Reflection;
using LCT.APICommunications.Model.AQL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using log4net.Core;
using LCT.Common;


namespace LCT.Services
{
    /// <summary>
    /// The JfrogService Class
    /// </summary>
    public class JFrogService : IJFrogService
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly IJfrogAqlApiCommunicationFacade m_JFrogApiCommunicationFacade;

        public JFrogService(IJfrogAqlApiCommunicationFacade jFrogApiCommunicationFacade)
        {
            m_JFrogApiCommunicationFacade = jFrogApiCommunicationFacade;
        }

        public async Task<IList<AqlResult>> GetInternalComponentDataByRepo(string repoName)
        {
            HttpResponseMessage? httpResponseMessage = null;
            IList<AqlResult> aqlResult = new List<AqlResult>();
            try
            {
                httpResponseMessage = await m_JFrogApiCommunicationFacade.GetInternalComponentDataByRepo(repoName);
                httpResponseMessage.EnsureSuccessStatusCode();
                string stringData = httpResponseMessage.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                var aqlResponse = JsonConvert.DeserializeObject<AqlResponse>(stringData);
                aqlResult = aqlResponse?.Results ?? new List<AqlResult>();
            }
            catch (HttpRequestException ex)
            {
                LogExceptionHandling.HttpException(ex, httpResponseMessage,"JFROG");
            }
            catch (InvalidOperationException ex)
            {
                LogExceptionHandling.InvalidOperationException(ex, "JFROG");
            }
            catch (TaskCanceledException ex)
            {
                LogExceptionHandling.TaskCanceledException(ex, "JFROG");
            }
            return aqlResult;
        }

        #nullable enable
        public async Task<AqlResult?> GetPackageInfo(string repoName, string packageName, string path)
        {
            HttpResponseMessage? httpResponseMessage = null;
            AqlResult? aqlResult = null;
            try
            {
                httpResponseMessage = await m_JFrogApiCommunicationFacade.GetPackageInfo(repoName, packageName, path);
                httpResponseMessage.EnsureSuccessStatusCode();

                string stringData = httpResponseMessage.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                var aqlResponse = JsonConvert.DeserializeObject<AqlResponse>(stringData);
                aqlResult = aqlResponse?.Results.FirstOrDefault();
            }
            catch (HttpRequestException httpException)
            {
                LogExceptionHandling.HttpException(httpException, httpResponseMessage, "JFROG");
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                LogExceptionHandling.InvalidOperationException(invalidOperationExcep, "JFROG");
            }
            catch (TaskCanceledException taskCancelledException)
            {
                LogExceptionHandling.TaskCanceledException(taskCancelledException, "JFROG");
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
