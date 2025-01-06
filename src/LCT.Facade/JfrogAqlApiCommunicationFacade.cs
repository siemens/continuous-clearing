// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.Common;
using LCT.Facade.Interfaces;
using System.Net.Http;
using System.Threading.Tasks;
using LCT.APICommunications.Model;

namespace LCT.Facade
{
    /// <summary>
    /// JFrogApiCommunicationFacade class
    /// </summary>
    public class JfrogAqlApiCommunicationFacade : IJfrogAqlApiCommunicationFacade
    {
        private readonly IJfrogAqlApiCommunication m_jfrogAqlApiCommunication;

        /// <summary>
        /// the JFrogApiCommunicationFacade method
        /// </summary>
        /// <param name="jfrogAqlApiCommunication"></param>
        public JfrogAqlApiCommunicationFacade(IJfrogAqlApiCommunication jfrogAqlApiCommunication)
        {
            m_jfrogAqlApiCommunication = jfrogAqlApiCommunication;
        }

        /// <summary>
        /// Checks connectivity with JFrog server
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        public Task<HttpResponseMessage> CheckConnection()
        {
            return m_jfrogAqlApiCommunication.CheckConnection();
        }

        /// <summary>
        /// Gets the Internal Component Data By Repo Name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>HttpResponseMessage</returns>
        public async Task<HttpResponseMessage> GetInternalComponentDataByRepo(string repoName)
        {
            return await m_jfrogAqlApiCommunication.GetInternalComponentDataByRepo(repoName);
        }
        /// <summary>
        /// Gets the Internal Component Data By Repo Name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>HttpResponseMessage</returns>
        public async Task<HttpResponseMessage> GetNpmComponentDataByRepo(string repoName)
        {
            return await m_jfrogAqlApiCommunication.GetNpmComponentDataByRepo(repoName);
        }
        /// <summary>
        /// Gets the Internal Component Data By Repo Name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>HttpResponseMessage</returns>
        public async Task<HttpResponseMessage> GetPypiComponentDataByRepo(string repoName)
        {
            return await m_jfrogAqlApiCommunication.GetPypiComponentDataByRepo(repoName);
        }

        /// <summary>
        /// Gets the package information in the repo, via the name or path
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <param name="packageName">repoName</param>
        /// <param name="path">repoName</param>
        /// <returns>AqlResult</returns>
        public async Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component)
        {
            return await m_jfrogAqlApiCommunication.GetPackageInfo(component);
        }        
    }
}
