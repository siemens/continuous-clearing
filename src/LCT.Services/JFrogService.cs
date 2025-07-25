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
            try
            {
                httpResponseMessage = await m_JFrogApiCommunicationFacade.GetInternalComponentDataByRepo(repoName);
                string truncateResponse = await LogHandlingHelper.TruncateTopLinesAsync(httpResponseMessage, 1000);
                LogHandlingHelper.HttpResponseOfStringContent("Get component data by repo", $"MethodName:GetInternalComponentDataByRepo()", truncateResponse, "");
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
                LogHandlingHelper.ExceptionErrorHandling("Get component data by repo", $"MethodName:GetInternalComponentDataByRepo()", httpException, "Check the JFrog server details ");
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get component data by repo", $"MethodName:GetInternalComponentDataByRepo(", invalidOperationExcep, "An invalid operation occurred while processing the request.");
            }
            catch (TaskCanceledException taskCancelledException)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get component data by repo", $"MethodName:GetInternalComponentDataByRepo()", taskCancelledException, "The request was canceled. This could be due to a timeout.");
            }

            return aqlResult;
        }
        public async Task<IList<AqlResult>> GetNpmComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepoAsync(m_JFrogApiCommunicationFacade.GetNpmComponentDataByRepo, repoName, "Get NPM component data by repo", "GetNpmComponentDataByRepo");
        }
        public async Task<IList<AqlResult>> GetPypiComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepoAsync(m_JFrogApiCommunicationFacade.GetPypiComponentDataByRepo, repoName, "Get Pypi component data by repo", "GetPypiComponentDataByRepo");
        }


#nullable enable
        public async Task<AqlResult?> GetPackageInfo(ComponentsToArtifactory component)
        {
            HttpResponseMessage? httpResponseMessage = null;
            AqlResult? aqlResult = null;
            try
            {
                httpResponseMessage = await m_JFrogApiCommunicationFacade.GetPackageInfo(component);
                string truncateResponse = await LogHandlingHelper.TruncateTopLinesAsync(httpResponseMessage, 1000);
                LogHandlingHelper.HttpResponseOfStringContent("Get package info", $"MethodName:GetPackageInfo()", truncateResponse, "");
                httpResponseMessage.EnsureSuccessStatusCode();

                string stringData = httpResponseMessage.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                var aqlResponse = JsonConvert.DeserializeObject<AqlResponse>(stringData);
                aqlResult = aqlResponse?.Results.FirstOrDefault();
            }
            catch (HttpRequestException httpException)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get package info", $"MethodName:GetPackageInfo()", httpException, "Check the JFrog server details or token validity.");
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get package info", $"MethodName:GetPackageInfo()", invalidOperationExcep, "An invalid operation occurred while processing the request.");
            }
            catch (TaskCanceledException taskCancelledException)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get package info", $"MethodName:GetPackageInfo()", taskCancelledException, "The request was canceled. This could be due to a timeout.");
            }

            return aqlResult;
        }

        public async Task<HttpResponseMessage> CheckJFrogConnectivity()
        {
            HttpResponseMessage? httpResponseMessage = null;
            httpResponseMessage = await m_JFrogApiCommunicationFacade.CheckConnection();

            return httpResponseMessage;
        }
        private static async Task<IList<AqlResult>> GetComponentDataByRepoAsync(Func<string, Task<HttpResponseMessage>> apiCall, string repoName, string operationName, string methodName)
        {
            HttpResponseMessage httpResponseMessage;
            IList<AqlResult> aqlResult = new List<AqlResult>();           

            try
            {
                httpResponseMessage = await apiCall(repoName);
                string truncateResponse = await LogHandlingHelper.TruncateTopLinesAsync(httpResponseMessage, 1000);
                LogHandlingHelper.HttpResponseOfStringContent(operationName, $"MethodName:{methodName}", truncateResponse, "");
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
                LogHandlingHelper.ExceptionErrorHandling(operationName, $"MethodName:{methodName}", httpException, "Check the JFrog server details");
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                LogHandlingHelper.ExceptionErrorHandling(operationName, $"MethodName:{methodName}", invalidOperationExcep, "An invalid operation occurred while processing the request.");
            }
            catch (TaskCanceledException taskCancelledException)
            {
                LogHandlingHelper.ExceptionErrorHandling(operationName, $"MethodName:{methodName}", taskCancelledException, "The request was canceled. This could be due to a timeout.");
            }

            return aqlResult;
        }
    }
}
