// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
//---------------------------------------------------------------------------------------------------------------------

using LCT.Common;
using LCT.APICommunications;
using LCT.ArtifactoryUploader.Model;
using log4net;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Net;
using CycloneDX.Models.Vulnerabilities;
using System;

namespace LCT.ArtifactoryUploader
{
    public class ArtifactoryValidator
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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
            }
            catch (HttpRequestException ex)
            {
                LogExceptionHandling.HttpException(ex, responseMessage, "JFROG");
            }
            catch (AggregateException ex)
            {
                LogExceptionHandling.AggregateException(ex, "JFROG");
            }
        }
    }
}
