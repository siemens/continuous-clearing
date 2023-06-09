// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.Facade.Interfaces
{
    /// <summary>
    /// The IJFrogApiCommunicationFacade interface
    /// </summary>
    public interface IJfrogAqlApiCommunicationFacade
    {
        /// <summary>
        /// Gets the Internal Component Data By Repo Name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage> GetInternalComponentDataByRepo(string repoName);
    }
}
