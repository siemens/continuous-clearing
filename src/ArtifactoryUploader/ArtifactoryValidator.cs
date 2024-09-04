// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
//---------------------------------------------------------------------------------------------------------------------

using LCT.Common;
using LCT.APICommunications;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace LCT.ArtifactoryUploader
{
    public class ArtifactoryValidator
    {
        private readonly NpmJfrogApiCommunication JfrogApiCommunication;

        public ArtifactoryValidator(NpmJfrogApiCommunication jfrogApiCommunication)
        {
            JfrogApiCommunication = jfrogApiCommunication;
        }

        public async Task ValidateArtifactoryCredentials(CommonAppSettings appSettings)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();
            try
            {
                responseMessage = await JfrogApiCommunication.GetApiKey();
                responseMessage.EnsureSuccessStatusCode();
            }
            catch(HttpRequestException ex)
            {
                ExceptionHandling.HttpException(ex,responseMessage, "Artifactory");
                CommonHelper.CallEnvironmentExit(-1);
            }
        }
    }
}
