// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.APICommunications.Interfaces
{
    /// <summary>
    /// The IJfrogAqlApiCommunication interface
    /// </summary>
    public interface IJfrogAqlApiCommunication
    {
        /// <summary>
        /// Gets the internal component data based on repo name
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage> GetInternalComponentDataByRepo(string repoName);
    }
}
