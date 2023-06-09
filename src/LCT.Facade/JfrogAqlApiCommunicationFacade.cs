// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.Facade.Interfaces;
using System.Net.Http;
using System.Threading.Tasks;

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
        /// Gets the Internal Component Data By Repo Name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>HttpResponseMessage</returns>
        public async Task<HttpResponseMessage> GetInternalComponentDataByRepo(string repoName)
        {
           return await m_jfrogAqlApiCommunication.GetInternalComponentDataByRepo(repoName);
        }
    }
}
