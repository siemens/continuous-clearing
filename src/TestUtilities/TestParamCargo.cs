using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestUtilities
{
    [ExcludeFromCodeCoverage]
    public class TestParamCargo
    {
        static readonly IConfiguration s_Config =
      new ConfigurationBuilder().AddJsonFile(@"appSettingsSW360IntegrationTest.json", true, true).Build();

        public string SW360AuthTokenType { get; set; }
        public string SW360AuthTokenValue { get; set; }
        public string SW360URL { get; set; }
        public string FossUrl { get; set; }
        public string SW360ProjectName { get; set; }
        public string SW360ProjectID { get; set; }
        public string ProjectType { get; set; }
        public string RemoveDevDependency { get; set; }
        public string FossologyFolderToUpload { get; set; }
        public string ArtifactoryUploadUser { get; set; }
        public string ArtifactoryUploadApiKey { get; set; }
        public string JfrogApi { get; set; }
        public string ThirdPartyDestinationRepoName { get; set; }
        public string InternalDestinationRepoName { get; set; }
        public string DevDestinationRepoName { get; set; }
        public string FossologyTrigger { get; set; }
        public string TelemetryEnable { get; set; }

        public TestParamCargo()
        {
            SW360AuthTokenType = s_Config["SW360AuthTokenType"];
            SW360AuthTokenValue = s_Config["SW360AuthTokenValue"];
            SW360URL = s_Config["SW360URL"];
            FossUrl = s_Config["FossologyURL"];
            SW360ProjectName = s_Config["SW360ProjectName"];
            SW360ProjectID = s_Config["SW360ProjectID"];
            ProjectType = "CARGO";
            RemoveDevDependency = s_Config["RemoveDevDependency"];
            ArtifactoryUploadUser = s_Config["ArtifactoryUploadUser"];
            ArtifactoryUploadApiKey = s_Config["ArtifactoryUploadApiKey"];
            JfrogApi = s_Config["JfrogApi"];
            ThirdPartyDestinationRepoName = "cargo-test";
            InternalDestinationRepoName = "cargo-test";
            DevDestinationRepoName = "cargo-test";
            FossologyTrigger = s_Config["EnableFossologyTrigger"];
            TelemetryEnable = s_Config["TelemetryEnable"];
        }
    }
}
