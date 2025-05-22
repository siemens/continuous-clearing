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
                LogHandlingHelper.HttpResponseHandling("Get component data by repo", $"MethodName:GetInternalComponentDataByRepo(),CorrelationId:{correlationId}", httpResponseMessage, "");
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
            HttpResponseMessage httpResponseMessage = null;
            IList<AqlResult> aqlResult = new List<AqlResult>();
            string correlationId = Guid.NewGuid().ToString();
            try
            {
                httpResponseMessage = await m_JFrogApiCommunicationFacade.GetNpmComponentDataByRepo(repoName, correlationId);
                LogHandlingHelper.HttpResponseHandling("Get NPM component data by repo", $"MethodName:GetNpmComponentDataByRepo(),CorrelationId:{correlationId}", httpResponseMessage, "");
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
                LogHandlingHelper.ExceptionErrorHandling("Get NPM component data by repo", $"MethodName:GetNpmComponentDataByRepo(),CorrelationId:{correlationId}", httpException, "Check the JFrog server details");
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get NPM component data by repo", $"MethodName:GetNpmComponentDataByRepo(),CorrelationId:{correlationId}", invalidOperationExcep, "An invalid operation occurred while processing the request.");
            }
            catch (TaskCanceledException taskCancelledException)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get NPM component data by repo", $"MethodName:GetNpmComponentDataByRepo(),CorrelationId:{correlationId}", taskCancelledException, "The request was canceled. This could be due to a timeout.");
            }

            return aqlResult;
        }
        public async Task<IList<AqlResult>> GetPypiComponentDataByRepo(string repoName)
        {
            HttpResponseMessage httpResponseMessage = null;
            IList<AqlResult> aqlResult = new List<AqlResult>();
            string correlationId = Guid.NewGuid().ToString();

            try
            {
                httpResponseMessage = await m_JFrogApiCommunicationFacade.GetPypiComponentDataByRepo(repoName, correlationId);
                LogHandlingHelper.HttpResponseHandling("Get Pypi component data by repo", $"MethodName:GetPypiComponentDataByRepo()", httpResponseMessage, "");
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
                LogHandlingHelper.ExceptionErrorHandling("Get Pypi component data by repo", $"MethodName:GetPypiComponentDataByRepo(),CorrelationId:{correlationId}", httpException, "Check the JFrog server details or token validity.");
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get Pypi component data by repo", $"MethodName:GetPypiComponentDataByRepo(),CorrelationId:{correlationId}", invalidOperationExcep, "An invalid operation occurred while processing the request.");
            }
            catch (TaskCanceledException taskCancelledException)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get Pypi component data by repo", $"MethodName:GetPypiComponentDataByRepo(),CorrelationId:{correlationId}", taskCancelledException, "The request was canceled. This could be due to a timeout.");
            }

            return aqlResult;
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
                LogHandlingHelper.HttpResponseHandling("Get package info", $"MethodName:GetPackageInfo(),CorrelationId:{correlationId}", httpResponseMessage, "");
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
    }
}
