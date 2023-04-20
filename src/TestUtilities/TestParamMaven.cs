using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestUtilities
{
    public class TestParamMaven
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
        public string ArtifactoryUploadUser { get; set; }
        public string ArtifactoryUploadApiKey { get; set; }
        public string JfrogApi { get; set; }
        public string DestinationRepoName { get; set; }

        public TestParamMaven()
        {
            SW360AuthTokenType = s_Config["SW360AuthTokenType"];
            SW360AuthTokenValue = s_Config["SW360AuthTokenValue"];
            SW360URL = s_Config["SW360URL"];
            FossUrl = s_Config["FossologyURL"];
            SW360ProjectName = s_Config["SW360ProjectName"];
            SW360ProjectID = s_Config["SW360ProjectID"];
            ProjectType = "MAVEN";
            RemoveDevDependency = s_Config["RemoveDevDependency"];
            ArtifactoryUploadUser = s_Config["ArtifactoryUploadUser"];
            ArtifactoryUploadApiKey = s_Config["ArtifactoryUploadApiKey"];
            JfrogApi = s_Config["JfrogApi"];
            DestinationRepoName = "maven-test";
        }
    }
}
