// --------------------------------------------------------------------------------------------------------------------
//  SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Collections.Generic;

namespace LCT.APICommunications
{
    /// <summary>
    /// Provides constant values used for API calls across the application.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static class ApiConstant
    {
        #region Fields

        /// <summary>
        /// The API suffix for SW360 component endpoints.
        /// </summary>
        public const string Sw360ComponentApiSuffix = "/resource/api/components";

        /// <summary>
        /// The API suffix for SW360 release endpoints.
        /// </summary>
        public const string Sw360ReleaseApiSuffix = "/resource/api/releases";

        /// <summary>
        /// The API suffix for SW360 projects endpoints.
        /// </summary>
        public const string Sw360ProjectsApiSuffix = "/resource/api/projects";

        /// <summary>
        /// The API suffix for SW360 users endpoints.
        /// </summary>
        public const string Sw360UsersSuffix = "/resource/api/users";

        /// <summary>
        /// The API suffix for SW360 resource endpoints.
        /// </summary>
        public const string Sw360ResourceApiSuffix = "/resource/";

        /// <summary>
        /// The API suffix for SW360 project URL endpoints.
        /// </summary>
        public const string Sw360ProjectUrlApiSuffix = "/group/guest/projects/-/project/detail/";

        /// <summary>
        /// The API suffix for SW360 release URL endpoints.
        /// </summary>
        public const string Sw360ReleaseUrlApiSuffix = "/group/guest/components/-/component/release/detailRelease/";

        /// <summary>
        /// The API endpoint for searching SW360 releases by external ID.
        /// </summary>
        public const string Sw360ReleaseByExternalId = "/resource/api/releases/searchByExternalIds";

        /// <summary>
        /// The API endpoint for searching SW360 components by external ID.
        /// </summary>
        public const string Sw360ComponentByExternalId = "/resource/api/components/searchByExternalIds";

        /// <summary>
        /// The API prefix for triggering Fossology process.
        /// </summary>
        public const string FossTriggerAPIPrefix = "/triggerFossologyProcess?uploadDescription=";

        /// <summary>
        /// The API suffix for Fossology trigger with outdated flag.
        /// </summary>
        public const string FossTriggerAPISuffix = "&markFossologyProcessOutdated=false";

        /// <summary>
        /// The MIME type for JSON content.
        /// </summary>
        public const string ApplicationJson = "application/json";

        /// <summary>
        /// The MIME type for all application content types.
        /// </summary>
        public const string ApplicationAllType = "application/*";

        /// <summary>
        /// The query parameter key for report format.
        /// </summary>
        public const string ReportFormat = "reportFormat";

        /// <summary>
        /// The URL query parameter for component name searches.
        /// </summary>
        public const string ComponentNameUrl = "?name=";

        /// <summary>
        /// The external ID prefix for NPM packages.
        /// </summary>
        public const string NPMExternalID = "pkg:npm/";

        /// <summary>
        /// The external ID prefix for NuGet packages.
        /// </summary>
        public const string NugetExternalID = "pkg:nuget/";

        /// <summary>
        /// The external ID prefix for Conan packages.
        /// </summary>
        public const string ConanExternalID = "pkg:conan/";

        /// <summary>
        /// The file extension for NPM packages.
        /// </summary>
        public const string NpmExtension = ".tgz";

        /// <summary>
        /// The file extension for NuGet packages.
        /// </summary>
        public const string NugetExtension = ".nupkg";

        /// <summary>
        /// The file extension for Debian packages.
        /// </summary>
        public const string DebianExtension = ".debian";

        /// <summary>
        /// The file extension for Maven source packages.
        /// </summary>
        public const string MavenExtension = "-sources.jar";

        /// <summary>
        /// The file extension for Python wheel packages.
        /// </summary>
        public const string PythonExtension = ".whl";

        /// <summary>
        /// The file extension for Cargo crate packages.
        /// </summary>
        public const string CargoExtension = ".crate";

        /// <summary>
        /// The API path for package storage information.
        /// </summary>
        public const string PackageInfoApi = "/api/storage/";

        /// <summary>
        /// The API path for copying packages.
        /// </summary>
        public const string CopyPackageApi = "/api/copy/";

        /// <summary>
        /// The API path for moving packages.
        /// </summary>
        public const string MovePackageApi = "/api/move/";

        /// <summary>
        /// The releases endpoint path segment.
        /// </summary>
        public const string Releases = "releases";

        /// <summary>
        /// The release endpoint path segment.
        /// </summary>
        public const string Release = "release";

        /// <summary>
        /// The query parameter key for folder ID.
        /// </summary>
        public const string FolderId = "folderId";

        /// <summary>
        /// The query parameter key for upload ID.
        /// </summary>
        public const string UploadId = "uploadId";

        /// <summary>
        /// The include filter keyword.
        /// </summary>
        public const string Include = "Include";

        /// <summary>
        /// The exclude filter keyword.
        /// </summary>
        public const string Exclude = "Exclude";

        /// <summary>
        /// The HTTP Accept header name.
        /// </summary>
        public const string Accept = "Accept";

        /// <summary>
        /// The attachment type for source files.
        /// </summary>
        public const string SOURCE = "SOURCE";

        /// <summary>
        /// The email field identifier.
        /// </summary>
        public const string Email = "Email";

        /// <summary>
        /// The OSS (Open Source Software) identifier.
        /// </summary>
        public const string Oss = "OSS";

        /// <summary>
        /// The HTTP POST method name.
        /// </summary>
        public const string POST = "POST";

        /// <summary>
        /// The public visibility identifier.
        /// </summary>
        public const string Public = "public";

        /// <summary>
        /// The Package URL identifier key.
        /// </summary>
        public const string PurlId = "package-url";

        /// <summary>
        /// The form field name for file input.
        /// </summary>
        public const string FileInput = "fileInput";

        /// <summary>
        /// The attachments endpoint path segment.
        /// </summary>
        public const string Attachments = "attachments";

        /// <summary>
        /// The HTTP Authorization header name.
        /// </summary>
        public const string Authorization = "Authorization";

        /// <summary>
        /// The JFrog API authentication header name.
        /// </summary>
        public const string JFrog_API_Header = "X-JFrog-Art-Api";

        /// <summary>
        /// The upload description parameter key.
        /// </summary>
        public const string UploadDescription = "uploadDescription";

        /// <summary>
        /// The additional data key for Fossology URL.
        /// </summary>
        public const string AdditionalDataFossologyURL = "fossology url";

        /// <summary>
        /// The default filename for attachment JSON files.
        /// </summary>
        public const string AttachmentJsonFileName = "Attachment.json";

        /// <summary>
        /// The API suffix for Fossology folders endpoint.
        /// </summary>
        public const string FossFoldersApiSuffix = "/repo/api/v1/folders";

        /// <summary>
        /// The API suffix for Fossology groups endpoint.
        /// </summary>
        public const string FossGroupsApiSuffix = "/repo/api/v1/groups";

        /// <summary>
        /// The API suffix for Fossology uploads endpoint.
        /// </summary>
        public const string FossUploadsApiSuffix = "/repo/api/v1/uploads";

        /// <summary>
        /// The API suffix for Fossology jobs endpoint.
        /// </summary>
        public const string FossJobsApiSuffix = "/repo/api/v1/jobs";

        /// <summary>
        /// The API suffix for Fossology report endpoint.
        /// </summary>
        public const string FossReportApiSuffix = "/repo/api/v1/report";

        /// <summary>
        /// The API suffix for Fossology users endpoint.
        /// </summary>
        public const string FossUsersApiSuffix = "/repo/api/v1/users";

        /// <summary>
        /// The URL suffix for Fossology upload job view.
        /// </summary>
        public const string FossUploadJobUrlSuffix = "/repo/?mod=view-license&upload=";

        /// <summary>
        /// The API suffix for searching SW360 releases by name.
        /// </summary>
        public const string Sw360ReleaseNameApiSuffix = "/resource/api/releases?name=";

        /// <summary>
        /// The API suffix for searching SW360 components by name.
        /// </summary>
        public const string Sw360ComponentNameApiSuffix = "/resource/api/components?name=";

        /// <summary>
        /// The API suffix for searching SW360 projects by tag with active state.
        /// </summary>
        public const string Sw360ProjectByTagApiSuffix = "/resource/api/projects?allDetails=true&_sw360_portlet_projects_STATE=ACTIVE&tag=";

        /// <summary>
        /// The error message for upload failures.
        /// </summary>
        public const string ErrorInUpload = "Error In Upload";

        /// <summary>
        /// The error message for invalid Artifactory configuration.
        /// </summary>
        public const string InvalidArtifactory = "Invalid artifactory";

        /// <summary>
        /// The error message when a package is not found.
        /// </summary>
        public const string PackageNotFound = "Package Not Found";

        /// <summary>
        /// The configuration key for Artifactory repository name.
        /// </summary>
        public const string ArtifactoryRepoName = "ArtifactoryRepoName";

        /// <summary>
        /// The API endpoint for JFrog Artifactory AQL search.
        /// </summary>
        public const string JfrogArtifactoryApiSearchAql = $"/api/search/aql";

        /// <summary>
        /// The list of retry intervals in seconds for API calls.
        /// </summary>
        public static readonly List<int> APIRetryIntervals = [5, 10, 30]; // in seconds

        #endregion Fields
    }
}
