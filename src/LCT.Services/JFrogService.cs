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
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly IJfrogAqlApiCommunicationFacade m_JFrogApiCommunicationFacade;

        public JFrogService(IJfrogAqlApiCommunicationFacade jFrogApiCommunicationFacade)
        {
            m_JFrogApiCommunicationFacade = jFrogApiCommunicationFacade;
        }

        public async Task<IList<AqlResult>> GetInternalComponentDataByRepo(string repoName)
        {
            HttpResponseMessage httpResponseMessage = null;
            IList<AqlResult> aqlResult = new List<AqlResult>();
            string correlationId = Guid.NewGuid().ToString();
            try
            {
                httpResponseMessage = await m_JFrogApiCommunicationFacade.GetInternalComponentDataByRepo(repoName, correlationId);
                string truncateResponse = await LogHandlingHelper.TruncateTopLinesAsync(httpResponseMessage,1000);
                LogHandlingHelper.HttpResponseOfStringContent("Get component data by repo", $"MethodName:GetInternalComponentDataByRepo(),CorrelationId:{correlationId}", truncateResponse, "");
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
                LogHandlingHelper.ExceptionErrorHandling("Get component data by repo", $"MethodName:GetInternalComponentDataByRepo(),CorrelationId:{correlationId}", httpException, "Check the JFrog server details ");
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get component data by repo", $"MethodName:GetInternalComponentDataByRepo(),CorrelationId:{correlationId}", invalidOperationExcep, "An invalid operation occurred while processing the request.");
            }
            catch (TaskCanceledException taskCancelledException)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get component data by repo", $"MethodName:GetInternalComponentDataByRepo(),CorrelationId:{correlationId}", taskCancelledException, "The request was canceled. This could be due to a timeout.");
            }

            return aqlResult;
        }
        public async Task<IList<AqlResult>> GetNpmComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepoAsync(m_JFrogApiCommunicationFacade.GetNpmComponentDataByRepo,repoName,"Get NPM component data by repo","GetNpmComponentDataByRepo");
        }
        public async Task<IList<AqlResult>> GetPypiComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepoAsync(m_JFrogApiCommunicationFacade.GetPypiComponentDataByRepo,repoName,"Get Pypi component data by repo","GetPypiComponentDataByRepo");
        }


#nullable enable
        public async Task<AqlResult?> GetPackageInfo(ComponentsToArtifactory component)
        {
            HttpResponseMessage? httpResponseMessage = null;
            AqlResult? aqlResult = null;
            string correlationId = Guid.NewGuid().ToString();
            try
            {
                httpResponseMessage = await m_JFrogApiCommunicationFacade.GetPackageInfo(component, correlationId);
                string truncateResponse = await LogHandlingHelper.TruncateTopLinesAsync(httpResponseMessage, 1000);
                LogHandlingHelper.HttpResponseOfStringContent("Get package info", $"MethodName:GetPackageInfo(),CorrelationId:{correlationId}", truncateResponse, "");
                httpResponseMessage.EnsureSuccessStatusCode();

                string stringData = httpResponseMessage.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                var aqlResponse = JsonConvert.DeserializeObject<AqlResponse>(stringData);
                aqlResult = aqlResponse?.Results.FirstOrDefault();
            }
            catch (HttpRequestException httpException)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get package info", $"MethodName:GetPackageInfo(),CorrelationId:{correlationId}", httpException, "Check the JFrog server details or token validity.");
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get package info", $"MethodName:GetPackageInfo(),CorrelationId:{correlationId}", invalidOperationExcep, "An invalid operation occurred while processing the request.");
            }
            catch (TaskCanceledException taskCancelledException)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get package info", $"MethodName:GetPackageInfo(),CorrelationId:{correlationId}", taskCancelledException, "The request was canceled. This could be due to a timeout.");
            }

            return aqlResult;
        }

        public async Task<HttpResponseMessage> CheckJFrogConnectivity(string correlationId)
        {
            HttpResponseMessage? httpResponseMessage = null;
            httpResponseMessage = await m_JFrogApiCommunicationFacade.CheckConnection(correlationId);

            return httpResponseMessage;
        }
        private async Task<IList<AqlResult>> GetComponentDataByRepoAsync( Func<string, string, Task<HttpResponseMessage>> apiCall,string repoName,string operationName,string methodName)
        {
            HttpResponseMessage httpResponseMessage = null;
            IList<AqlResult> aqlResult = new List<AqlResult>();
            string correlationId = Guid.NewGuid().ToString();

            try
            {
                httpResponseMessage = await apiCall(repoName, correlationId);
                string truncateResponse = await LogHandlingHelper.TruncateTopLinesAsync(httpResponseMessage, 1000);
                LogHandlingHelper.HttpResponseOfStringContent(operationName, $"MethodName:{methodName},CorrelationId:{correlationId}", truncateResponse, "");
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
                LogHandlingHelper.ExceptionErrorHandling(operationName, $"MethodName:{methodName},CorrelationId:{correlationId}", httpException, "Check the JFrog server details");
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                LogHandlingHelper.ExceptionErrorHandling(operationName, $"MethodName:{methodName},CorrelationId:{correlationId}", invalidOperationExcep, "An invalid operation occurred while processing the request.");
            }
            catch (TaskCanceledException taskCancelledException)
            {
                LogHandlingHelper.ExceptionErrorHandling(operationName, $"MethodName:{methodName},CorrelationId:{correlationId}", taskCancelledException, "The request was canceled. This could be due to a timeout.");
            }

            return aqlResult;
        }
    }
}
