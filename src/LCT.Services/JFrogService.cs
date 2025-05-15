// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.Common.Logging;
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
                LogHandling.LogHttpResponseDetails("Get component data by repo", "GetInternalComponentDataByRepo()", httpResponseMessage, "");
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
                LogHandling.HttpErrorHandelingForLog("Get component data by repo", "GetInternalComponentDataByRepo()", httpException, "Check the JFrog server details ");
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                LogHandling.HttpErrorHandelingForLog("Get component data by repo", "GetInternalComponentDataByRepo()", invalidOperationExcep, "An invalid operation occurred while processing the request.");
            }
            catch (TaskCanceledException taskCancelledException)
            {
                LogHandling.HttpErrorHandelingForLog("Get component data by repo", "GetInternalComponentDataByRepo()", taskCancelledException, "The request was canceled. This could be due to a timeout.");
            }

            return aqlResult;
        }
        public async Task<IList<AqlResult>> GetNpmComponentDataByRepo(string repoName)
        {
            HttpResponseMessage httpResponseMessage = null;
            IList<AqlResult> aqlResult = new List<AqlResult>();

            try
            {
                httpResponseMessage = await m_JFrogApiCommunicationFacade.GetNpmComponentDataByRepo(repoName);
                LogHandling.LogHttpResponseDetails("Get NPM component data by repo", "GetNpmComponentDataByRepo()", httpResponseMessage, "");
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
                LogHandling.HttpErrorHandelingForLog("Get NPM component data by repo", "GetNpmComponentDataByRepo()", httpException, "Check the JFrog server details");
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                LogHandling.HttpErrorHandelingForLog("Get NPM component data by repo", "GetNpmComponentDataByRepo()", invalidOperationExcep, "An invalid operation occurred while processing the request.");
            }
            catch (TaskCanceledException taskCancelledException)
            {
                LogHandling.HttpErrorHandelingForLog("Get NPM component data by repo", "GetNpmComponentDataByRepo()", taskCancelledException, "The request was canceled. This could be due to a timeout.");
            }

            return aqlResult;
        }
        public async Task<IList<AqlResult>> GetPypiComponentDataByRepo(string repoName)
        {
            HttpResponseMessage httpResponseMessage = null;
            IList<AqlResult> aqlResult = new List<AqlResult>();

            try
            {
                httpResponseMessage = await m_JFrogApiCommunicationFacade.GetPypiComponentDataByRepo(repoName);
                LogHandling.LogHttpResponseDetails("Get Pypi component data by repo", "GetPypiComponentDataByRepo()", httpResponseMessage, "");
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
                LogHandling.HttpErrorHandelingForLog("Get Pypi component data by repo", "GetPypiComponentDataByRepo()", httpException, "Check the JFrog server details or token validity.");
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                LogHandling.HttpErrorHandelingForLog("Get Pypi component data by repo", "GetPypiComponentDataByRepo()", invalidOperationExcep, "An invalid operation occurred while processing the request.");
            }
            catch (TaskCanceledException taskCancelledException)
            {
                LogHandling.HttpErrorHandelingForLog("Get Pypi component data by repo", "GetPypiComponentDataByRepo()", taskCancelledException, "The request was canceled. This could be due to a timeout.");
            }

            return aqlResult;
        }


#nullable enable
        public async Task<AqlResult?> GetPackageInfo(ComponentsToArtifactory component)
        {
            HttpResponseMessage? httpResponseMessage = null;
            AqlResult? aqlResult = null;
            try
            {
                httpResponseMessage = await m_JFrogApiCommunicationFacade.GetPackageInfo(component);
                LogHandling.LogHttpResponseDetails("Get package info", "GetPackageInfo()", httpResponseMessage, "");
                httpResponseMessage.EnsureSuccessStatusCode();

                string stringData = httpResponseMessage.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                var aqlResponse = JsonConvert.DeserializeObject<AqlResponse>(stringData);
                aqlResult = aqlResponse?.Results.FirstOrDefault();
            }
            catch (HttpRequestException httpException)
            {
                LogHandling.HttpErrorHandelingForLog("Get package info", "GetPackageInfo()", httpException, "Check the JFrog server details or token validity.");
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                LogHandling.HttpErrorHandelingForLog("Get package info", "GetPackageInfo()", invalidOperationExcep, "An invalid operation occurred while processing the request.");
            }
            catch (TaskCanceledException taskCancelledException)
            {
                LogHandling.HttpErrorHandelingForLog("Get package info", "GetPackageInfo()", taskCancelledException, "The request was canceled. This could be due to a timeout.");
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
