// --------------------------------------------------------------------------------------------------------------------
//  SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.APICommunications
{
    /// <summary>
    /// constans need for all the api calls
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static class ApiConstant
    {
        public const string Sw360ComponentApiSuffix = "/resource/api/components";
        public const string Sw360ReleaseApiSuffix = "/resource/api/releases";
        public const string Sw360ProjectsApiSuffix = "/resource/api/projects";
        public const string Sw360UsersSuffix = "/resource/api/users";
        public const string Sw360ResourceApiSuffix = "/resource/";
        public const string Sw360ProjectUrlApiSuffix = "/group/guest/projects/-/project/detail/";
        public const string Sw360ReleaseUrlApiSuffix = "/group/guest/components/-/component/release/detailRelease/";
        public const string Sw360ReleaseByExternalId = "/resource/api/releases/searchByExternalIds";
        public const string Sw360ComponentByExternalId = "/resource/api/components/searchByExternalIds";
        public const string FossTriggerAPIPrefix = "/triggerFossologyProcess?uploadDescription=";
        public const string FossTriggerAPISuffix = "&markFossologyProcessOutdated=false";
        public const string ApplicationJson = "application/json";
        public const string ApplicationAllType = "application/*";
        public const string ReportFormat = "reportFormat";
        public const string ComponentNameUrl = "?name=";
        public const string NPMExternalID = "pkg:npm/";
        public const string NugetExternalID = "pkg:nuget/";
        public const string NpmExtension = ".tgz";
        public const string NugetExtension = ".nupkg";
        public const string MavenExtension = "-sources.jar";
        public const string PackageInfoApi = "/api/storage/";
        public const string CopyPackageApi = "/api/copy/";
        public const string Releases = "releases";
        public const string Release = "release";
        public const string FolderId = "folderId";
        public const string UploadId = "uploadId";
        public const string Include = "Include";
        public const string Exclude = "Exclude";
        public const string Accept = "Accept";
        public const string SOURCE = "SOURCE";
        public const string Email = "Email";
        public const string Oss = "OSS";
        public const string POST = "POST";
        public const string Public = "public";
        public const string PurlId = "package-url";
        public const string FileInput = "fileInput";
        public const string Attachments = "attachments";
        public const string Authorization = "Authorization";
        public const string JFrog_API_Header = "X-JFrog-Art-Api";
        public const string UploadDescription = "uploadDescription";
        public const string AdditionalDataFossologyURL = "fossology url";
        public const string AttachmentJsonFileName = "Attachment.json";
        public const string FossFoldersApiSuffix = "/repo/api/v1/folders";
        public const string FossGroupsApiSuffix = "/repo/api/v1/groups";
        public const string FossUploadsApiSuffix = "/repo/api/v1/uploads";
        public const string FossJobsApiSuffix = "/repo/api/v1/jobs";
        public const string FossReportApiSuffix = "/repo/api/v1/report";
        public const string FossUsersApiSuffix = "/repo/api/v1/users";
        public const string FossUploadJobUrlSuffix = "/repo/?mod=view-license&upload=";
        public const string Sw360ReleaseNameApiSuffix = "/resource/api/releases?name=";
        public const string Sw360ComponentNameApiSuffix = "/resource/api/components?name=";
        public const string Sw360ProjectByTagApiSuffix = "/resource/api/projects?allDetails=true&_sw360_portlet_projects_STATE=ACTIVE&tag=";
        public const string ErrorInUpload = "Error In Upload";
        public const string InvalidArtifactory = "Invalid artifactory";
        public const string PackageNotFound = "Package Not Found";
        public const string ArtifactoryRepoName = "ArtifactoryRepoName";
        public const string JfrogArtifactoryApiSearchAql = $"/api/search/aql";
    }
}
