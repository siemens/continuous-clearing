// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
//---------------------------------------------------------------------------------------------------------------------

using LCT.APICommunications.Interfaces;
using LCT.Common;
using System;
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
            string correlationId = Guid.NewGuid().ToString();
            try
            {
                responseMessage = await _JfrogAqlApiCommunication.CheckConnection(correlationId);
                LogHandlingHelper.HttpResponseHandling("JFrog Connection Validation", $"Methodname-ValidateArtifactoryCredentials():CorrelationId-{correlationId}", responseMessage, "");
                responseMessage.EnsureSuccessStatusCode();
                return 0;
            }
            catch (HttpRequestException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Get sw360 Project details for validating", $"MethodName-GetProjectById():CorrelationId-{correlationId}", ex, "");
                ExceptionHandling.HttpException(ex, responseMessage, "Artifactory");
                return -1;
            }
        }
    }
}
