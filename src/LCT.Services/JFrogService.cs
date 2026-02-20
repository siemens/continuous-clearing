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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.Services
{
    /// <summary>
    /// The JfrogService Class
    /// </summary>
    public class JFrogService : IJFrogService
    {
        readonly IJfrogAqlApiCommunicationFacade m_JFrogApiCommunicationFacade;

        public JFrogService(IJfrogAqlApiCommunicationFacade jFrogApiCommunicationFacade)
        {
            m_JFrogApiCommunicationFacade = jFrogApiCommunicationFacade;
        }

        /// <summary>
        /// gets component data by repo
        /// </summary>
        /// <param name="apiCall"></param>
        /// <param name="repoName"></param>
        /// <returns>list of result</returns>
        private static async Task<IList<AqlResult>> GetComponentDataByRepo(Func<string, Task<HttpResponseMessage>> apiCall, string repoName)
        {
            HttpResponseMessage httpResponseMessage = null;
            IList<AqlResult> aqlResult = new List<AqlResult>();

            try
            {
                httpResponseMessage = await apiCall(repoName);
                await LogHandlingHelper.HttpResponseHandling("Response of component data", $"MethodName:GetInternalComponentDataByRepo()", httpResponseMessage);
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
                LogHandlingHelper.ExceptionErrorHandling("HttpRequestException getting component data from repo", $"MethodName:GetInternalComponentDataByRepo()", httpException, "Check the JFrog server details ");
            }
            catch (InvalidOperationException invalidOperationExcep)
            {
                LogHandlingHelper.ExceptionErrorHandling("InvalidOperationException getting component data from repo", $"MethodName:GetInternalComponentDataByRepo(", invalidOperationExcep, "An invalid operation occurred while processing the request.");
            }
            catch (TaskCanceledException taskCancelledException)
            {
                LogHandlingHelper.ExceptionErrorHandling("TaskCanceledException getting component data from repo", $"MethodName:GetInternalComponentDataByRepo()", taskCancelledException, "The request was canceled. This could be due to a timeout.");
            }

            return aqlResult;
        }

        /// <summary>
        /// gets internal component data by repo
        /// </summary>
        /// <param name="repoName"></param>
        /// <returns>list of result</returns>
        public async Task<IList<AqlResult>> GetInternalComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(m_JFrogApiCommunicationFacade.GetInternalComponentDataByRepo, repoName);
        }

        /// <summary>
        /// gets npm component data by repo
        /// </summary>
        /// <param name="repoName"></param>
        /// <returns>list of aql result</returns>
        public async Task<IList<AqlResult>> GetNpmComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(m_JFrogApiCommunicationFacade.GetNpmComponentDataByRepo, repoName);
        }

        /// <summary>
        /// gets pypi component data by repo
        /// </summary>
        /// <param name="repoName"></param>
        /// <returns>list of aql result</returns>
        public async Task<IList<AqlResult>> GetPypiComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(m_JFrogApiCommunicationFacade.GetPypiComponentDataByRepo, repoName);
        }

        /// <summary>
        /// Retrieves a collection of Cargo component data for the specified repository.
        /// </summary>
        /// <param name="repoName">The name of the repository from which to retrieve Cargo component data. Cannot be null or empty.</param>
        /// <returns>A list of <see cref="AqlResult"/> objects containing Cargo component data for the specified repository. The
        /// list will be empty if no components are found.</returns>
        public async Task<IList<AqlResult>> GetCargoComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(m_JFrogApiCommunicationFacade.GetCargoComponentDataByRepo, repoName);
        }

        /// <summary>
        /// Retrieves package information from Artifactory for the specified component.
        /// </summary>       
        /// <param name="component">The component for which to retrieve package information. Must specify valid identifiers for the target
        /// Artifactory package.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="AqlResult"/>
        /// instance with package details if found; otherwise, <see langword="null"/>.</returns>
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

        /// <summary>
        /// checks jfrog connectivity
        /// </summary>
        /// <returns>task that represents operation</returns>
        public async Task<HttpResponseMessage> CheckJFrogConnectivity()
        {
            HttpResponseMessage? httpResponseMessage = null;
            httpResponseMessage = await m_JFrogApiCommunicationFacade.CheckConnection();

            return httpResponseMessage;
        }
    }
}
