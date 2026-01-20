// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
//---------------------------------------------------------------------------------------------------------------------

using LCT.APICommunications.Interfaces;
using LCT.Common;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.ArtifactoryUploader
{
    public class ArtifactoryValidator
    {
        #region Fields

        private readonly IJfrogAqlApiCommunication _JfrogAqlApiCommunication;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ArtifactoryValidator class.
        /// </summary>
        /// <param name="jfrogAqlApiCommunication">The JFrog AQL API communication instance.</param>
        public ArtifactoryValidator(IJfrogAqlApiCommunication jfrogAqlApiCommunication)
        {
            _JfrogAqlApiCommunication = jfrogAqlApiCommunication;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Asynchronously validates Artifactory credentials by checking the connection.
        /// </summary>
        /// <returns>A task containing 0 if validation succeeds, -1 if validation fails.</returns>
        public async Task<int> ValidateArtifactoryCredentials()
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();
            try
            {
                responseMessage = await _JfrogAqlApiCommunication.CheckConnection();
                responseMessage.EnsureSuccessStatusCode();
                return 0;
            }
            catch (HttpRequestException ex)
            {
                ExceptionHandling.HttpException(ex, responseMessage, "Artifactory");
                return -1;
            }
        }

        #endregion
    }
}
