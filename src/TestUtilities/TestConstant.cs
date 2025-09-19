// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace TestUtilities
{
    [ExcludeFromCodeCoverage]
    public static class TestConstant
    {
        private static readonly TestParam s_testParamObj = new TestParam();
        private static readonly TestParamNuget s_testParamObjnuget = new TestParamNuget();

        public static readonly string Sw360ComponentApi = $"{s_testParamObj.SW360URL}/resource/api/components";
        public static readonly string Sw360ReleaseApi = $"{s_testParamObj.SW360URL}/resource/api/releases";
        public static readonly string JfrogApi = $"{s_testParamObj.JfrogApi}/api/storage/{s_testParamObj.ThirdPartyDestinationRepoName}";
        public static readonly string JfrogApiNuget = $"{s_testParamObjnuget.JfrogApi}/api/storage/{s_testParamObjnuget.ThirdPartyDestinationRepoName}";



        public static readonly string TestSw360TokenType = s_testParamObj.SW360AuthTokenType;
        public static readonly string TestSw360TokenValue = s_testParamObj.SW360AuthTokenValue;


        public const string componentNameUrl = "?name=";
        public const string PackageFilePath = "--Directory:InputFolder";
        public const string BomFolderPath = "--Directory:OutputFolder";
        public const string Sw360Token = "--SW360:Token";
        public const string BomFilePath = "--Directory:BomFilePath";
        public const string SW360URL = "--SW360:URL";
        public const string FossologyURL = "--SW360:Fossology:URL";
        public const string EnableFossologyTrigger = "--SW360:Fossology:EnableTrigger";
        public const string SW360AuthTokenType = "--SW360:AuthTokenType";
        public const string SW360ProjectName = "--SW360:ProjectName";
        public const string SW360ProjectID = "--SW360:ProjectID";
        public const string ProjectType = "--ProjectType";
        public const string RemoveDevDependency = "--SW360:IgnoreDevDependency";
        public const string Mode = "--Mode";
        public const string JFrog_API_Header = "X-JFrog-Art-Api";
        public const string Email = "Email";
        public const string ArtifactoryKey = "--Jfrog:Token";
        public const string ArtifactoryUser = "--artifactoryuploaduser";
        public const string TelemetryEnable = "--Telemetry:Enable";

        public const string JfrogNpmThirdPartyDestRepoName = "--Npm:Artifactory:ThirdPartyRepos:0:Name";
        public const string JfrogNpmInternalRepo = "--Npm:Artifactory:InternalRepos:0";
        public const string JfrogNpmInternalDestRepoName = "--Npm:ReleaseRepo ";
        public const string JfrogNpmDevDestRepoName = "--Npm:DevDepRepo ";

        public const string JfrogMavenThirdPartyDestRepoName = "--Maven:Artifactory:ThirdPartyRepos:0:Name";
        public const string JfrogMavenInternalRepo = "--Maven:Artifactory:InternalRepos:0";
        public const string JfrogMavenInternalDestRepoName = "--Maven:ReleaseRepo ";
        public const string JfrogMavenDevDestRepoName = "--Maven:DevDepRepo ";

        public const string JfrogNugetThirdPartyDestRepoName = "--Nuget:Artifactory:ThirdPartyRepos:0:Name";
        public const string JfrogNugetInternalRepo = "--Nuget:Artifactory:InternalRepos:0";
        public const string JfrogNugetInternalDestRepoName = "--Nuget:ReleaseRepo ";
        public const string JfrogNugetDevDestRepoName = "--Nuget:DevDepRepo ";

        public const string JfrogPythonThirdPartyDestRepoName = "--Poetry:Artifactory:ThirdPartyRepos:0:Name";
        public const string JfrogPoetryInternalRepo = "--Poetry:Artifactory:InternalRepos:0";
        public const string JfrogPythonInternalDestRepoName = "--Poetry:ReleaseRepo ";
        public const string JfrogPythonDevDestRepoName = "--Poetry:DevDepRepo ";

        public const string JfrogConanThirdPartyDestRepoName = "--Conan:Artifactory:ThirdPartyRepos:0:Name";
        public const string JfrogConanInternalRepo = "--Conan:Artifactory:InternalRepos:0";
        public const string JfrogConanInternalDestRepoName = "--Conan:ReleaseRepo ";
        public const string JfrogConanDevDestRepoName = "--Conan:DevDepRepo ";
        public const string JfrogDebianInternalRepo = "--Debian:Artifactory:InternalRepos:0";

        public const string JfrogChocoThirdPartyDestRepoName = "--Choco:Artifactory:ThirdPartyRepos:0:Name";
        public const string JfrogChocoInternalRepo = "--Choco:Artifactory:InternalRepos:0";
        public const string JfrogChocoInternalDestRepoName = "--Choco:ReleaseRepo ";
        public const string JfrogChocoDevDestRepoName = "--Choco:DevDepRepo ";

        public const string NuspecMode = "--NuspecMode";
        public const string JFrogApiURL = "--JFrog:URL";
        public const string DryRun = "--JFrog:DryRun";
        public const string Appsettings = "--settingsfilepath";
    }
}
