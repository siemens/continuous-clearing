// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Microsoft.Extensions.Configuration;

namespace TestUtilities
{
    public class TestParamDebian
    {
        static readonly IConfiguration s_Config = new ConfigurationBuilder().AddJsonFile(@"appSettingsSW360IntegrationTest.json", true, true).Build();

        public string SW360AuthTokenType { get; set; }
        public string SW360AuthTokenValue { get; set; }
        public string SW360URL { get; set; }
        public string FossUrl { get; set; }
        public string SW360ProjectName { get; set; }
        public string SW360ProjectID { get; set; }
        public string ProjectType { get; set; }
        public string RemoveDevDependency { get; set; }
        public string ArtifactoryUploadUser { get; set; }
        public string ArtifactoryUploadApiKey { get; set; }
        public string JfrogApi { get; set; }
        public string FossologyTrigger { get; set; }
        public string TelemetryEnable { get; set; }
        public string BasicSBOM { get; set; }
        public string BasicSBOMEnable { get; set; }

        public TestParamDebian()
        {
            SW360AuthTokenType = s_Config["SW360AuthTokenType"];
            SW360AuthTokenValue = s_Config["SW360AuthTokenValue"];
            SW360URL = s_Config["SW360URL"];
            FossUrl = s_Config["FossologyURL"];
            SW360ProjectName = s_Config["SW360ProjectName"];
            SW360ProjectID = s_Config["SW360ProjectID"];
            ProjectType = "Debian";
            ArtifactoryUploadUser = s_Config["ArtifactoryUploadUser"];
            ArtifactoryUploadApiKey = s_Config["ArtifactoryUploadApiKey"];
            JfrogApi = s_Config["JfrogApi"];
            RemoveDevDependency = s_Config["RemoveDevDependency"];
            FossologyTrigger = s_Config["EnableFossologyTrigger"];
            TelemetryEnable = s_Config["TelemetryEnable"];
            BasicSBOM = s_Config["BasicSBOM"];
            BasicSBOMEnable = s_Config["BasicSBOMEnable"];
        }
    }
}
