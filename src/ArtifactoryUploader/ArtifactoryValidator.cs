// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
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
        private readonly IJfrogAqlApiCommunication _JfrogAqlApiCommunication;

        public ArtifactoryValidator(IJfrogAqlApiCommunication jfrogAqlApiCommunication)
        {
            _JfrogAqlApiCommunication = jfrogAqlApiCommunication;
        }

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
    }
}
