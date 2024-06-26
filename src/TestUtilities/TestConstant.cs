﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
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
        public const string PackageFilePath = "--packageFilePath";
        public const string BomFolderPath = "--bomFolderPath";
        public const string Sw360Token = "--sw360Token";
        public const string BomFilePath = "--bomFilePath";
        public const string SW360URL = "--sW360URL";
        public const string FossologyURL = "--fossologyURL";
        public const string SW360AuthTokenType = "--sW360AuthTokenType";
        public const string SW360ProjectName = "--sW360ProjectName";
        public const string SW360ProjectID = "--sW360ProjectID";
        public const string ProjectType = "--projectType";
        public const string RemoveDevDependency = "--removeDevDependency";
        public const string Mode = "--Mode";
        public const string JFrog_API_Header = "X-JFrog-Art-Api";
        public const string Email = "Email";
        public const string ArtifactoryUser = "--artifactoryuploaduser";
        public const string ArtifactoryKey = "--artifactoryuploadapikey";

        public const string JfrogNpmThirdPartyDestRepoName = "--npm:JfrogThirdPartyDestRepoName ";
        public const string JfrogNpmInternalDestRepoName = "--npm:JfrogInternalDestRepoName ";
        public const string JfrogNpmDevDestRepoName = "--npm:JfrogDevDestRepoName ";

        public const string JfrogMavenThirdPartyDestRepoName = "--maven:JfrogThirdPartyDestRepoName ";
        public const string JfrogMavenInternalDestRepoName = "--maven:JfrogInternalDestRepoName ";
        public const string JfrogMavenDevDestRepoName = "--maven:JfrogDevDestRepoName ";

        public const string JfrogNugetThirdPartyDestRepoName = "--nuget:JfrogThirdPartyDestRepoName ";
        public const string JfrogNugetInternalDestRepoName = "--nuget:JfrogInternalDestRepoName ";
        public const string JfrogNugetDevDestRepoName = "--nuget:JfrogDevDestRepoName ";

        public const string JfrogPythonThirdPartyDestRepoName = "--python:JfrogThirdPartyDestRepoName ";
        public const string JfrogPythonInternalDestRepoName = "--python:JfrogInternalDestRepoName ";
        public const string JfrogPythonDevDestRepoName = "--python:JfrogDevDestRepoName ";

        public const string JfrogConanThirdPartyDestRepoName = "--conan:JfrogThirdPartyDestRepoName ";
        public const string JfrogConanInternalDestRepoName = "--conan:JfrogInternalDestRepoName ";
        public const string JfrogConanDevDestRepoName = "--conan:JfrogDevDestRepoName ";

        public const string NuspecMode = "--NuspecMode";
        public const string JFrogApiURL = "--JFrogApi";
        public const string CycloneDxSBomTemplatePath = "--cycloneDxSBomTemplatePath";
        public const string Release = "--release";
    }
}
