// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Logging;
using LCT.Common.Model;
using LCT.Facade;
using LCT.Facade.Interfaces;
using LCT.Services;
using LCT.Services.Interface;
using LCT.SW360PackageCreator.Interfaces;
using LCT.SW360PackageCreator.Model;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Level = log4net.Core.Level;

namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// CreatorHelper class
    /// </summary>
    public class CreatorHelper(IDictionary<string, IPackageDownloader> packageDownloderList) : ICreatorHelper
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly List<Components> lstReleaseNotCreated = new List<Components>();
        List<Components> componentsAvailableInSw360 = new List<Components>();
        List<Components> DuplicateComponentsByPurlId = new List<Components>();
        private const string SOURCE = "SOURCE";
        private readonly IDictionary<string, IPackageDownloader> _packageDownloderList = packageDownloderList;

        /// <summary>
        /// Get Download Url Not Found List
        /// </summary>
        /// <param name="comparisionBomDataList"></param>
        /// <returns>BOM data</returns>
        public List<ComparisonBomData> GetDownloadUrlNotFoundList(List<ComparisonBomData> comparisionBomDataList)
        {
            List<ComparisonBomData> downloadUrlNotFoundList = new List<ComparisonBomData>();

            foreach (ComparisonBomData comparionBomData in comparisionBomDataList)
            {
                if (comparionBomData.ApprovedStatus == Dataconstant.Approved)
                {
                    Logger.Debug($"Component {comparionBomData.Name} with version {comparionBomData.Version} is skipped from the 'list of components without a source download URL' because it is in the approved state.");
                    continue;
                }
                if ((comparionBomData.DownloadUrl == Dataconstant.DownloadUrlNotFound || string.IsNullOrEmpty(comparionBomData.DownloadUrl)) && !comparionBomData.SourceAttachmentStatus)
                {
                    comparionBomData.ReleaseID = string.IsNullOrEmpty(comparionBomData.ReleaseLink) ?
                        comparionBomData.ReleaseID : CommonHelper.GetSubstringOfLastOccurance(comparionBomData.ReleaseLink, "/");
                    downloadUrlNotFoundList.Add(comparionBomData);
                }
            }

            return downloadUrlNotFoundList;
        }

        /// <summary>
        /// Download Release Attachment Source
        /// </summary>
        /// <param name="component"></param>
        /// <returns>url list</returns>
        public async Task<Dictionary<string, string>> DownloadReleaseAttachmentSource(ComparisonBomData component)
        {
            Dictionary<string, string> AttachmentUrlList = new Dictionary<string, string>();
            string localPathforDownload = GetDownloadPathForComponetType(component);
            Logger.DebugFormat("DownloadReleaseAttachmentSource():local path for download release attachment:{0}", localPathforDownload);
            if (!component.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["MAVEN"]))
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Logger.DebugFormat("DownloadReleaseAttachmentSource()-Name-{0},version-{1},localPathforDownload-{2}", component.Name, component.Version, localPathforDownload);
                LogSourceAndDownloadUrlWarnings(component);

                if (component.DownloadUrl.Equals(Dataconstant.DownloadUrlNotFound))
                {
                    Logger.DebugFormat("DownloadReleaseAttachmentSource():Source file is not attached,Release source Download Url is not Found for {0}-{1}", component.Name, component.Version);
                }
                else
                {
                    await GetAttachmentUrlList(component, AttachmentUrlList, localPathforDownload);
                }
            }
            else
            {
                await DownloadDependencyList(component);
                GetAttachmentUrlListForMvn(localPathforDownload, component, ref AttachmentUrlList);

            }
            if (AttachmentUrlList.Count > 0)
            {
                Logger.DebugFormat("Attachments found for ComponentName: {0}, Version: {1}", component.Name, component.Version);
                foreach (var attachment in AttachmentUrlList)
                {
                    Logger.DebugFormat("DownloadReleaseAttachmentSource(): Attachment Key: {0}, Attachment URL: {1}", attachment.Key, attachment.Value);
                }
            }
            else
            {
                Logger.DebugFormat("DownloadReleaseAttachmentSource(): No attachments found for ComponentName: {0}, Version: {1}", component.Name, component.Version);
            }
            return AttachmentUrlList;
        }

        /// <summary>
        /// get attachment url list
        /// </summary>
        /// <param name="component"></param>
        /// <param name="AttachmentUrlList"></param>
        /// <param name="localPathforDownload"></param>
        /// <returns>path</returns>
        private async Task<string> GetAttachmentUrlList(ComparisonBomData component, Dictionary<string, string> AttachmentUrlList, string localPathforDownload)
        {
            string downloadPath = string.Empty;
            if (component.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["DEBIAN"]))
            {
                if (!string.IsNullOrEmpty(component.SourceUrl))
                {
                    downloadPath = await _packageDownloderList["DEBIAN"].DownloadPackage(component, localPathforDownload);
                }
            }
            else if (component.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["POETRY"]))
            {
                downloadPath = await GetAttachmentUrlList(component, localPathforDownload);
                downloadPath = ConvertZipToTarGzIfNeeded(downloadPath);

            }
            else if (component.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["CONAN"]))
            {
                downloadPath = await GetAttachmentUrlList(component, localPathforDownload);
            }
            else if (component.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["CARGO"]))
            {
                downloadPath = await DownloadCargoSource(component, localPathforDownload);
            }
            else if (component.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["ALPINE"]))
            {
                if (!string.IsNullOrEmpty(component.SourceUrl))
                {
                    downloadPath = await _packageDownloderList["ALPINE"].DownloadPackage(component, localPathforDownload);
                }
            }
            else
            {
                downloadPath = await _packageDownloderList["NPM"].DownloadPackage(component, localPathforDownload);
            }
            if (!string.IsNullOrEmpty(downloadPath))
            {
                AttachmentUrlList.Add(SOURCE, downloadPath);
            }
            Logger.DebugFormat("GetAttachmentUrlList():Downloaded release attachment path:Name-{0},Version-{1},DownloadPath-{2}", component.Name, component.Version, downloadPath);
            return downloadPath;
        }

        /// <summary>
        /// Download Cargo Source
        /// </summary>
        /// <param name="component"></param>
        /// <param name="localPathforDownload"></param>
        /// <returns>data</returns>
        private static async Task<string> DownloadCargoSource(ComparisonBomData component, string localPathforDownload)
        {
            if (!string.IsNullOrEmpty(component.DownloadUrl))
            {
                string fileName = $"{component.Name}-{component.Version}.crate";
                string downloadFilePath = Path.Combine(localPathforDownload, fileName);

                string directoryPath = Path.GetDirectoryName(downloadFilePath);
                if (!string.IsNullOrEmpty(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                Uri uri = new Uri(component.DownloadUrl);
                await UrlHelper.DownloadFileAsync(uri, downloadFilePath);
                if (downloadFilePath.EndsWith(FileConstant.CrateFileExtension))
                {
                    string tarfile = Path.ChangeExtension(downloadFilePath, FileConstant.TargzFileExtension);
                    File.Copy(downloadFilePath, tarfile, true);
                    downloadFilePath = tarfile;
                }

                return downloadFilePath;
            }
            return "";
        }
        /// <summary>
        /// Convert Zip To TarGz If Needed
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>file path</returns>
        private static string ConvertZipToTarGzIfNeeded(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return filePath;
            if (filePath.EndsWith(FileConstant.ZipFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                string tarGzFilePath = Path.ChangeExtension(filePath, FileConstant.TargzFileExtension);
                File.Copy(filePath, tarGzFilePath, true);
                return tarGzFilePath;
            }
            return filePath;
        }

        /// <summary>
        /// Download  Dependency List
        /// </summary>
        /// <param name="component"></param>
        /// <returns>task that returns asynchronous operation</returns>
        private static async Task DownloadDependencyList(ComparisonBomData component)
        {
            string localPathforDownload = $"{Path.GetTempPath()}ClearingTool\\DownloadedFiles/";
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            Process p = new();
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            Logger.DebugFormat("DownloadDependencyList(): Start - ComponentName: {0}, Version: {1}, Group: {2}", component.Name, component.Version, component.Group);
            if (isWindows)
            {
                p.StartInfo.FileName = Path.Combine(@"cmd.exe");
                p.StartInfo.Arguments = $"/c mvn org.apache.maven.plugins:maven-dependency-plugin:copy -Dartifact={component.Group}:{component.Name}:{component.Version}:jar:sources -DoutputDirectory={localPathforDownload}";
                Logger.DebugFormat("DownloadDependencyList(): Windows OS detected. Command: {0}", p.StartInfo.Arguments);
            }
            else
            {
                p.StartInfo.FileName = Path.Combine(@"mvn");
                p.StartInfo.Arguments = $"org.apache.maven.plugins:maven-dependency-plugin:copy -Dartifact={component.Group}:{component.Name}:{component.Version}:jar:sources -DoutputDirectory={localPathforDownload}";
                Logger.DebugFormat("DownloadDependencyList(): Non-Windows OS detected. Command: {0}", p.StartInfo.Arguments);
            }

            var processResult = ProcessAsyncHelper.RunAsync(p.StartInfo);
            await processResult;
        }

        /// <summary>
        /// Gets Attachment Url List For Mvn
        /// </summary>
        /// <param name="localPathforDownload"></param>
        /// <param name="component"></param>
        /// <param name="attachmentUrlList"></param>
        public static void GetAttachmentUrlListForMvn(string localPathforDownload, ComparisonBomData component,
                                                      ref Dictionary<string, string> attachmentUrlList)
        {
            localPathforDownload = $"{localPathforDownload}{component.Name}-{component.Version}-sources.jar";

            if (File.Exists(localPathforDownload))
            {
                attachmentUrlList.Add(SOURCE, localPathforDownload);
            }

        }

        /// <summary>
        /// Gets Attachment Url List
        /// </summary>
        /// <param name="component"></param>
        /// <param name="localPathforDownload"></param>
        /// <returns>task that returns asynchronous operation</returns>
        private static async Task<string> GetAttachmentUrlList(ComparisonBomData component, string localPathforDownload)
        {
            string downloadPath = string.Empty;
            try
            {
                string componenetFullName = UrlHelper.GetCorrectFileExtension(component.SourceUrl);
                string downloadFilePath = $"{localPathforDownload}{componenetFullName}";
                Directory.CreateDirectory(Path.GetDirectoryName(downloadFilePath));

                if (!string.IsNullOrEmpty(component.SourceUrl) && !component.SourceUrl.Equals(Dataconstant.SourceUrlNotFound))
                {
                    Uri uri = new Uri(component.SourceUrl);
                    downloadPath = await UrlHelper.DownloadFileAsync(uri, downloadFilePath);
                }
            }
            catch (WebException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetAttachmentUrlList", $"MethodName:GetAttachmentUrlList(), Release Name: {component.Name}@{component.Version}, SourceUrl: {component.SourceUrl}", ex, "A network error occurred while trying to download the attachment.");
            }
            catch (UriFormatException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetAttachmentUrlList", $"MethodName:GetAttachmentUrlList(), Release Name: {component.Name}@{component.Version}, SourceUrl: {component.SourceUrl}", ex, "The provided URL is not in a valid format.");
            }

            return downloadPath;
        }

        /// <summary>
        /// Sets Contents For Comparison BOM
        /// </summary>
        /// <param name="lstComponentForBOM"></param>
        /// <param name="sw360Service"></param>
        /// <returns>BOM data</returns>
        public async Task<List<ComparisonBomData>> SetContentsForComparisonBOM(List<Components> lstComponentForBOM, ISW360Service sw360Service)
        {
            Logger.Debug($"SetContentsForComparisonBOM():Starting to identify available components data in SW360");
            Logger.Logger.Log(null, Level.Notice, $"Collecting BoM Data...", null);
            componentsAvailableInSw360 = await sw360Service.GetAvailableReleasesInSw360(lstComponentForBOM);
            DuplicateComponentsByPurlId = sw360Service.GetDuplicateComponentsByPurlId();
            //Checking components count before getting status of individual comp details
            List<ComparisonBomData> comparisonBomData = await GetComparisionBomItems(lstComponentForBOM, sw360Service);

            LogHandlingHelper.SW360AvailableComponentsList(componentsAvailableInSw360);
            Logger.Debug($"SetContentsForComparisonBOM():Completed of the sw360 data for available components");
            return comparisonBomData;
        }       

        /// <summary>
        /// Gets Comparision Bom Items
        /// </summary>
        /// <param name="lstComponentForBOM"></param>
        /// <param name="sw360Service"></param>
        /// <returns>BOM data</returns>
        private async Task<List<ComparisonBomData>> GetComparisionBomItems(List<Components> lstComponentForBOM, ISW360Service sw360Service)
        {
            List<ComparisonBomData> comparisonBomData = new();
            ComparisonBomData mapper;

            foreach (Components item in lstComponentForBOM)
            {
                mapper = new ComparisonBomData();
                ReleasesInfo releasesInfo = await GetReleaseInfoFromSw360(item, componentsAvailableInSw360, sw360Service);
                IRepository repo = new Repository();

                mapper.Name = item.Name;
                mapper.Group = item.Group;
                mapper.Version = item.Version;
                mapper.ComponentExternalId = item.ComponentExternalId;
                mapper.ReleaseExternalId = item.ReleaseExternalId;
                mapper.SourceUrl = item.SourceUrl;
                mapper.DownloadUrl = item.DownloadUrl;
                mapper.ComponentStatus = GetComponentAvailabilityStatus(componentsAvailableInSw360, item);
                mapper.ReleaseStatus = IsReleaseAvailable(item.Name, item.Version, item.ReleaseExternalId);
                mapper.AlpineSource = item.AlpineSourceData;

                if (!string.IsNullOrEmpty(item.ReleaseExternalId) && item.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["DEBIAN"]))
                {
                    SetDebianUrls(item, releasesInfo, mapper);
                }
                else if (!string.IsNullOrEmpty(item.ReleaseExternalId) && item.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["MAVEN"]))
                {
                    mapper.DownloadUrl = GetMavenDownloadUrl(mapper, item, releasesInfo);
                }
                else if (!string.IsNullOrEmpty(item.ReleaseExternalId) && IsOtherPackageType(item))
                {
                    mapper.DownloadUrl = mapper.SourceUrl;
                }
                else
                {
                    mapper.DownloadUrl = GetComponentDownloadUrl(mapper, item, repo, releasesInfo);
                }
                SetMapperStatus(mapper, releasesInfo);
                Logger.Debug($"Sw360 availability status for Name {mapper.Name}: {mapper.ComponentExternalId}={mapper.ComponentStatus} - Version {mapper.Version}: {mapper.ReleaseExternalId}={mapper.ReleaseStatus}");
                comparisonBomData.Add(mapper);
            }
            return comparisonBomData;
        }

        /// <summary>
        /// Sets Debia nUrls
        /// </summary>
        /// <param name="item"></param>
        /// <param name="releasesInfo"></param>
        /// <param name="mapper"></param>
        private static void SetDebianUrls(Components item, ReleasesInfo releasesInfo, ComparisonBomData mapper)
        {
            if ((string.IsNullOrEmpty(item.SourceUrl) || item.SourceUrl == Dataconstant.SourceUrlNotFound) && !string.IsNullOrEmpty(releasesInfo.SourceCodeDownloadUrl))
            {
                // If not able to get source details from snapshot.org, try getting source URL from SW360
                mapper.SourceUrl = releasesInfo.SourceCodeDownloadUrl;
                mapper.DownloadUrl = releasesInfo.SourceCodeDownloadUrl;
            }
            mapper.PatchURls = item.PatchURLs;
        }

        /// <summary>
        /// package type
        /// </summary>
        /// <param name="item"></param>
        /// <returns>boolean value</returns>
        private static bool IsOtherPackageType(Components item)
        {
            return item.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["POETRY"]) ||
                   item.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["CONAN"]) ||
                   item.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["ALPINE"]) ||
                   item.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["CARGO"]);
        }

        /// <summary>
        /// Sets Mapper Status
        /// </summary>
        /// <param name="mapper"></param>
        /// <param name="releasesInfo"></param>
        private void SetMapperStatus(ComparisonBomData mapper, ReleasesInfo releasesInfo)
        {
            mapper.ApprovedStatus = GetApprovedStatus(mapper.ComponentStatus, mapper.ReleaseStatus, releasesInfo);
            mapper.IsComponentCreated = GetCreatedStatus(mapper.ComponentStatus);
            mapper.IsReleaseCreated = GetCreatedStatus(mapper.ReleaseStatus);
            mapper.FossologyUploadStatus = GetFossologyUploadStatus(mapper.ApprovedStatus);
            mapper.ReleaseAttachmentLink = string.Empty;
            mapper.ReleaseLink = GetReleaseLink(componentsAvailableInSw360, mapper.Name, mapper.Version);
            mapper.FossologyLink = releasesInfo?.AdditionalData?.TryGetValue("fossology url", out string fossologyUrl) == true ? fossologyUrl : string.Empty;
            mapper.ReleaseCreatedBy = releasesInfo?.CreatedBy ?? string.Empty;
        }

        /// <summary>
        /// Gets Maven Download Url
        /// </summary>
        /// <param name="mapper"></param>
        /// <param name="item"></param>
        /// <param name="releasesInfo"></param>
        /// <returns></returns>
        public static string GetMavenDownloadUrl(ComparisonBomData mapper, Components item, ReleasesInfo releasesInfo)
        {
            string sourceURL = string.Empty;

            //IF Release already exists in SW360 , tool will not update any field.
            //Hence do not need to find DOWNLOAD URL here and this REDUCE the exeuction time

            if (mapper.ReleaseStatus.Equals(Dataconstant.Available))
            {
                return releasesInfo?.SourceCodeDownloadUrl ?? string.Empty;
            }
            else
            {
                sourceURL = $"{CommonAppSettings.SourceURLMavenApi}{item.Group}/{item.Name}/{item.Version}/{item.Name}-{item.Version}-sources.jar";
            }
            return sourceURL;
        }

        /// <summary>
        /// Gets Updated Components Details
        /// </summary>
        /// <param name="ListofBomComponents"></param>
        /// <param name="updatedCompareBomData"></param>
        /// <param name="sw360Service"></param>
        /// <param name="bom"></param>
        /// <returns>bom data</returns>
        public async Task<Bom> GetUpdatedComponentsDetails(List<Components> ListofBomComponents, List<ComparisonBomData> updatedCompareBomData,
            ISW360Service sw360Service, Bom bom)
        {
            //To get latest ReleaseLinks after component creation
            componentsAvailableInSw360 = await sw360Service.GetAvailableReleasesInSw360(ListofBomComponents);

            foreach (ComparisonBomData comBom in updatedCompareBomData)
            {
                UpdateReleaseLink(comBom);
                try
                {
                    var propertiesToUpdate = new Dictionary<string, string>
            {
                { Dataconstant.Cdx_ClearingState, comBom.ApprovedStatus },
                { Dataconstant.Cdx_ReleaseUrl, comBom.ReleaseLink },
                { Dataconstant.Cdx_FossologyUrl, comBom.FossologyLink ?? "" }
            };

                    Component component = FindMatchingComponent(bom, comBom);
                    AddOrUpdateProperties(component, propertiesToUpdate);
                }
                catch (JsonSerializationException ex)
                {
                    LogHandlingHelper.ExceptionErrorHandling("GetUpdatedComponentsDetails", $"MethodName:GetUpdatedComponentsDetails(), ComponentName:{comBom.Name}, Version:{comBom.Version}", ex, "An error occurred while serializing or deserializing JSON data for the component.");
                }
            }
            return bom;
        }

        /// <summary>
        /// Finds Matching Component
        /// </summary>
        /// <param name="bom"></param>
        /// <param name="comBom"></param>
        /// <returns>component data</returns>
        private static Component FindMatchingComponent(Bom bom, ComparisonBomData comBom)
        {
            if (!bom.Components.Exists(x => x.BomRef.Contains(Dataconstant.PurlCheck()["MAVEN"])))
            {
                return bom.Components.Find(com =>
                    string.IsNullOrEmpty(com.Group)
                        ? com.Name == comBom.Name && com.Version.Contains(comBom.Version)
                        : $"{com.Group}{Dataconstant.ForwardSlash}{com.Name}" == comBom.Name && com.Version.Contains(comBom.Version));
            }
            else
            {
                return bom.Components.Find(com => com.Name == comBom.Name && com.Version.Contains(comBom.Version));
            }
        }

        /// <summary>
        /// Updates Release Link
        /// </summary>
        /// <param name="comBom"></param>
        private void UpdateReleaseLink(ComparisonBomData comBom)
        {
            if (string.IsNullOrEmpty(comBom.ReleaseLink))
            {
                comBom.ReleaseLink = GetReleaseLink(componentsAvailableInSw360, comBom.Name, comBom.Version);
            }
        }

        /// <summary>
        /// Add Or Update Properties
        /// </summary>
        /// <param name="component"></param>
        /// <param name="listOfProperties"></param>
        private static void AddOrUpdateProperties(Component component, Dictionary<string, string> listOfProperties)
        {
            if (component == null) return;
            foreach (var property in listOfProperties)
            {
                var properties = component.Properties;
                CommonHelper.RemoveDuplicateAndAddProperty(ref properties, property.Key, property.Value);
                component.Properties = properties;
            }

        }

        /// <summary>
        /// Gets Download Path For Componet Type
        /// </summary>
        /// <param name="component"></param>
        /// <returns>local path</returns>
        private static string GetDownloadPathForComponetType(ComparisonBomData component)
        {
            string localPathforDownload = string.Empty;
            try
            {
                if (component.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["DEBIAN"]))
                {
                    localPathforDownload = $"{Directory.GetParent(Directory.GetCurrentDirectory())}/ClearingTool/DownloadedFiles/";
                }
                else if (component.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["ALPINE"]))
                {
                    localPathforDownload = $"{Directory.GetParent(Directory.GetCurrentDirectory())}/ClearingTool/DownloadedFiles/";
                }
                else
                {
                    localPathforDownload = $"{Path.GetTempPath()}/ClearingTool/DownloadedFiles/";
                }
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetDownloadPathForComponetType", $"MethodName:GetDownloadPathForComponetType(), ComponentName:{component.Name}, ReleaseExternalId:{component.ReleaseExternalId}", ex, "An I/O error occurred while determining the download path for the component.");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GetDownloadPathForComponetType", $"MethodName:GetDownloadPathForComponetType(), ComponentName:{component.Name}, ReleaseExternalId:{component.ReleaseExternalId}", ex, "Unauthorized access occurred while determining the download path for the component.");
                Logger.Error($"GetDownloadPathForComponetType() ", ex);
            }

            return localPathforDownload;
        }

        /// <summary>
        /// Gets Creator Kpi Data
        /// </summary>
        /// <param name="updatedCompareBomData"></param>
        /// <returns>kpi data</returns>
        public CreatorKpiData GetCreatorKpiData(List<ComparisonBomData> updatedCompareBomData)
        {
            CreatorKpiData creatorKpiData = new CreatorKpiData
            {
                ComponentsReadFromComparisonBOM = ComponentCreator.TotalComponentsFromPackageIdentifier,
                TotalDuplicateAndInValidComponents = ComponentCreator.TotalComponentsFromPackageIdentifier >= updatedCompareBomData.Count ?
                ComponentCreator.TotalComponentsFromPackageIdentifier - updatedCompareBomData.Count : 0
            };

            foreach (ComparisonBomData item in updatedCompareBomData)
            {
                if (IsComponentNewlyCreated(item))
                {
                    creatorKpiData.ComponentsOrReleasesCreatedNewlyInSw360++;
                }

                if (IsComponentCreated(item))
                {
                    creatorKpiData.ComponentsOrReleasesExistingInSw360++;
                }

                if (IsComponentNotCreated(item))
                {
                    creatorKpiData.ComponentsOrReleasesNotCreatedInSw360++;
                }

                ComponentsWithAndWithOutSourceDownloadUrl(ref creatorKpiData, item);
            }

            Program.CreatorStopWatch.Stop();
            creatorKpiData.TimeTakenByComponentCreator =
                (int)Program.CreatorStopWatch.Elapsed.TotalSeconds;

            return creatorKpiData;
        }

        /// <summary>
        /// Components With And With Out Source Download Url
        /// </summary>
        /// <param name="creatorKpiData"></param>
        /// <param name="item"></param>
        public static void ComponentsWithAndWithOutSourceDownloadUrl(ref CreatorKpiData creatorKpiData, ComparisonBomData item)
        {
            if (item.DownloadUrl == Dataconstant.DownloadUrlNotFound || string.IsNullOrEmpty(item.DownloadUrl))
            {
                creatorKpiData.ComponentsWithoutSourceDownloadUrl++;
            }

            if (item.DownloadUrl != Dataconstant.DownloadUrlNotFound && !string.IsNullOrEmpty(item.DownloadUrl))
            {
                creatorKpiData.ComponentsWithSourceDownloadUrl++;
            }

            if (item.DownloadUrl == Dataconstant.DownloadUrlNotFound && item.DownloadUrl == Dataconstant.PackageUrlNotFound)
            {
                creatorKpiData.ComponentsWithoutSourceAndPackageUrl++;
            }

            if (item.DownloadUrl == Dataconstant.PackageUrlNotFound || string.IsNullOrEmpty(item.DownloadUrl))
            {
                creatorKpiData.ComponentsWithoutPackageUrl++;
            }
            if (item.FossologyUploadStatus == Dataconstant.NotUploaded)
            {
                creatorKpiData.ComponentsNotUploadedInFossology++;
            }
            if (item.FossologyUploadStatus == Dataconstant.Uploaded)
            {
                creatorKpiData.ComponentsUploadedInFossology++;
            }
        }

        /// <summary>
        /// Is Component Not Created
        /// </summary>
        /// <param name="item"></param>
        /// <returns>boolean value</returns>
        private static bool IsComponentNotCreated(ComparisonBomData item)
        {
            return item.IsComponentCreated == Dataconstant.NotCreated || item.IsReleaseCreated == Dataconstant.NotCreated;
        }

        /// <summary>
        /// Is component created
        /// </summary>
        /// <param name="item"></param>
        /// <returns>boolean value</returns>
        private static bool IsComponentCreated(ComparisonBomData item)
        {
            return item.IsComponentCreated == Dataconstant.Created && item.IsReleaseCreated == Dataconstant.Created;
        }

        /// <summary>
        /// Is Component Newly Created
        /// </summary>
        /// <param name="item"></param>
        /// <returns>boolean value</returns>
        private static bool IsComponentNewlyCreated(ComparisonBomData item)
        {
            return (item.IsComponentCreated == Dataconstant.NewlyCreated && item.IsReleaseCreated == Dataconstant.NewlyCreated)
                                || (item.IsComponentCreated == Dataconstant.Created && item.IsReleaseCreated == Dataconstant.NewlyCreated);
        }

        /// <summary>
        /// Initialize Sw360 Creator Service
        /// </summary>
        /// <param name="appSettings"></param>
        /// <returns>sw360 creator service</returns>
        public static ISw360CreatorService InitializeSw360CreatorService(CommonAppSettings appSettings)
        {
            SW360ConnectionSettings sw360ConnectionSettings = new SW360ConnectionSettings()
            {
                SW360URL = appSettings.SW360.URL,
                SW360AuthTokenType = appSettings.SW360.AuthTokenType,
                Sw360Token = appSettings.SW360.Token,
                IsTestMode = appSettings.IsTestMode,
                Timeout = appSettings.TimeOut
            };
            var sw360ApicommunicationFacade = new SW360ApicommunicationFacade(sw360ConnectionSettings);
            return new Sw360CreatorService(sw360ApicommunicationFacade);
        }

        /// <summary>
        /// initialize project service
        /// </summary>
        /// <param name="appSettings"></param>
        /// <returns>project service</returns>
        public static ISw360ProjectService InitializeSw360ProjectService(CommonAppSettings appSettings)
        {
            SW360ConnectionSettings sw360ConnectionSettings = new SW360ConnectionSettings()
            {
                SW360URL = appSettings.SW360.URL,
                SW360AuthTokenType = appSettings.SW360.AuthTokenType,
                Sw360Token = appSettings.SW360.Token,
                IsTestMode = appSettings.IsTestMode,
                Timeout = appSettings.TimeOut
            };
            ISW360ApicommunicationFacade sW360ApicommunicationFacade = new SW360ApicommunicationFacade(sw360ConnectionSettings);

            return new Sw360ProjectService(sW360ApicommunicationFacade);
        }

        /// <summary>
        /// write creator kpt data to console
        /// </summary>
        /// <param name="creatorKpiData"></param>
        public void WriteCreatorKpiDataToConsole(CreatorKpiData creatorKpiData)
        {
            Logger.Warn("Todo: Default component type is OSS. User is expected to manually change the component type from OSS to COTS.");
            KpiNames createrKpiNames = IdentifyKpiNames(creatorKpiData);
            Dictionary<string, int> printList = new Dictionary<string, int>()
            {
                {createrKpiNames.ComponentsFromBOM,creatorKpiData.ComponentsReadFromComparisonBOM },

                { createrKpiNames.ReleasesCreatedInSW360,creatorKpiData.ComponentsOrReleasesCreatedNewlyInSw360},

                { createrKpiNames.ReleasesExistsInSW360,creatorKpiData.ComponentsOrReleasesExistingInSw360},

                {createrKpiNames.ReleasesNotCreatedInSW360,creatorKpiData.ComponentsOrReleasesNotCreatedInSw360},

                { createrKpiNames.ReleasesWithoutSourceDownloadURL,creatorKpiData.ComponentsWithoutSourceDownloadUrl},

                { createrKpiNames.ReleasesWithSourceDownloadURL,creatorKpiData.ComponentsWithSourceDownloadUrl},

                {createrKpiNames.ComponentsWithoutPackageURL,creatorKpiData.ComponentsWithoutPackageUrl},

                {createrKpiNames.ComponentsWithoutSourceAndPackageURL,creatorKpiData.ComponentsWithoutSourceAndPackageUrl},

                {createrKpiNames.ComponentsUploadedInFOSSology,creatorKpiData.ComponentsUploadedInFossology},

                {createrKpiNames.ComponentsNotUploadedInFOSSology,creatorKpiData.ComponentsNotUploadedInFossology},

                {createrKpiNames.TotalDuplicateAndInValidComponents,creatorKpiData.TotalDuplicateAndInValidComponents}
            };

            Dictionary<string, double> printTimingList = new Dictionary<string, double>()
            {
                { "ComponentCreator",creatorKpiData.TimeTakenByComponentCreator }
            };

            LoggerHelper.WriteToConsoleTable(printList, printTimingList, "", Dataconstant.Creator, createrKpiNames);
        }

        /// <summary>
        /// identify kpi names
        /// </summary>
        /// <param name="creatorKpiData"></param>
        /// <returns>kpi names</returns>
        private static KpiNames IdentifyKpiNames(CreatorKpiData creatorKpiData)
        {
            KpiNames createrKpiNames = new KpiNames();
            createrKpiNames.ComponentsFromBOM = CommonHelper.Convert(creatorKpiData, nameof(creatorKpiData.ComponentsReadFromComparisonBOM));
            createrKpiNames.ReleasesCreatedInSW360 = CommonHelper.Convert(creatorKpiData, nameof(creatorKpiData.ComponentsOrReleasesCreatedNewlyInSw360));
            createrKpiNames.ReleasesExistsInSW360 = CommonHelper.Convert(creatorKpiData, nameof(creatorKpiData.ComponentsOrReleasesExistingInSw360));
            createrKpiNames.ReleasesNotCreatedInSW360 = CommonHelper.Convert(creatorKpiData, nameof(creatorKpiData.ComponentsOrReleasesNotCreatedInSw360));
            createrKpiNames.ReleasesWithSourceDownloadURL = CommonHelper.Convert(creatorKpiData, nameof(creatorKpiData.ComponentsWithSourceDownloadUrl));
            createrKpiNames.ReleasesWithoutSourceDownloadURL = CommonHelper.Convert(creatorKpiData, nameof(creatorKpiData.ComponentsWithoutSourceDownloadUrl));
            createrKpiNames.ComponentsWithoutSourceAndPackageURL = CommonHelper.Convert(creatorKpiData, nameof(creatorKpiData.ComponentsWithoutSourceAndPackageUrl));
            createrKpiNames.TotalDuplicateAndInValidComponents = CommonHelper.Convert(creatorKpiData, nameof(creatorKpiData.TotalDuplicateAndInValidComponents));
            createrKpiNames.ComponentsNotUploadedInFOSSology = CommonHelper.Convert(creatorKpiData, nameof(creatorKpiData.ComponentsNotUploadedInFossology));
            createrKpiNames.ComponentsUploadedInFOSSology = CommonHelper.Convert(creatorKpiData, nameof(creatorKpiData.ComponentsUploadedInFossology));
            createrKpiNames.ComponentsWithoutPackageURL = CommonHelper.Convert(creatorKpiData, nameof(creatorKpiData.ComponentsWithoutPackageUrl));


            return createrKpiNames;
        }

        /// <summary>
        /// Write Source Not Found List To Console
        /// </summary>
        /// <param name="comparisionBomDataList"></param>
        /// <param name="appSetting"></param>
        public void WriteSourceNotFoundListToConsole(List<ComparisonBomData> comparisionBomDataList, CommonAppSettings appSetting)
        {          

            List<ComparisonBomData> sourceNotAvailable = GetDownloadUrlNotFoundList(comparisionBomDataList);
            foreach (var item in comparisionBomDataList)
            {
                if (item.IsReleaseCreated == Dataconstant.NotCreated || item.IsComponentCreated == Dataconstant.NotCreated)
                {
                    Components releasenotCreated = new Components
                    {
                        Name = item.Name,
                        Version = item.Version
                    };
                    lstReleaseNotCreated.Add(releasenotCreated);
                }
            }

            // Removes common components
            sourceNotAvailable.RemoveAll(src => lstReleaseNotCreated.Any(rls => src.Name == rls.Name && src.Version == rls.Version));

            LoggerHelper.WriteComponentsWithoutDownloadURLToKpi(sourceNotAvailable, lstReleaseNotCreated, appSetting.SW360.URL, DuplicateComponentsByPurlId);
        }

        /// <summary>
        /// Gets Component Availability Status
        /// </summary>
        /// <param name="componentsAvailable"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        private static string GetComponentAvailabilityStatus(List<Components> componentsAvailable, Components component)
        {
            return componentsAvailable.Exists(x => x.Name.Equals(component.Name, StringComparison.InvariantCultureIgnoreCase)
            || x.ComponentExternalId.Equals(component.ComponentExternalId, StringComparison.InvariantCultureIgnoreCase)) ? Dataconstant.Available : Dataconstant.NotAvailable;
        }

        /// <summary>
        /// Is Release Available
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="componentVersion"></param>
        /// <param name="releaseExternalId"></param>
        /// <returns>data</returns>
        private string IsReleaseAvailable(string componentName, string componentVersion, string releaseExternalId)
        {
            if (componentsAvailableInSw360.Exists(
                x => (x.Name.Equals(componentName, StringComparison.InvariantCultureIgnoreCase) && x.Version.Equals(componentVersion, StringComparison.InvariantCultureIgnoreCase))
                || x.ReleaseExternalId.Equals(releaseExternalId, StringComparison.InvariantCultureIgnoreCase)))
            {
                return Dataconstant.Available;
            }

            return Dataconstant.NotAvailable;
        }

        /// <summary>
        /// Gets Component Download Url
        /// </summary>
        /// <param name="mapper"></param>
        /// <param name="item"></param>
        /// <param name="repo"></param>
        /// <param name="releasesInfo"></param>
        /// <returns>data</returns>
        public static string GetComponentDownloadUrl(ComparisonBomData mapper, Components item, IRepository repo, ReleasesInfo releasesInfo)
        {

            if (mapper.ReleaseStatus.Equals(Dataconstant.Available))
            {
                return !string.IsNullOrEmpty(releasesInfo?.SourceCodeDownloadUrl) ? releasesInfo.SourceCodeDownloadUrl : repo.FormGitCloneUrl(mapper.SourceUrl, item.Name, item.Version);
            }
            return repo.FormGitCloneUrl(mapper.SourceUrl, item.Name, item.Version);
        }

        /// <summary>
        /// Gets Approved Status
        /// </summary>
        /// <param name="componentAvailabelStatus"></param>
        /// <param name="releaseAvailbilityStatus"></param>
        /// <param name="releasesInfo"></param>
        /// <returns>data</returns>
        public static string GetApprovedStatus(string componentAvailabelStatus, string releaseAvailbilityStatus, ReleasesInfo releasesInfo)
        {

            if (componentAvailabelStatus == Dataconstant.Available && releaseAvailbilityStatus == Dataconstant.Available)
            {
                return releasesInfo?.ClearingState ?? Dataconstant.NotAvailable;
            }

            return Dataconstant.NotAvailable;
        }

        /// <summary>
        /// Gets Created Status
        /// </summary>
        /// <param name="availabilityStatus"></param>
        /// <returns>status</returns>
        public static string GetCreatedStatus(string availabilityStatus)
        {
            return availabilityStatus == Dataconstant.Available ? Dataconstant.Created : Dataconstant.NotCreated;
        }

        /// <summary>
        /// Gets Fossology Upload Status
        /// </summary>
        /// <param name="ComponentApprovedStatus"></param>
        /// <returns>status</returns>
        public static string GetFossologyUploadStatus(string ComponentApprovedStatus)
        {
            return (ComponentApprovedStatus == Dataconstant.NotAvailable ||
                     ComponentApprovedStatus == Dataconstant.NewClearing) ? Dataconstant.NotUploaded : Dataconstant.AlreadyUploaded;
        }

        /// <summary>
        /// Gets Release Link
        /// </summary>
        /// <param name="componentsAvailableInSw360"></param>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns>release link</returns>
        public static string GetReleaseLink(List<Components> componentsAvailableInSw360, string name, string version)
        {
            string releaseLink = componentsAvailableInSw360.Where(x => x.Name.Trim().Equals(name.Trim(), StringComparison.CurrentCultureIgnoreCase)
            && x.Version.Trim().Equals(version.Trim(), StringComparison.CurrentCultureIgnoreCase)).Select(x => x.ReleaseLink).FirstOrDefault();

            if (releaseLink == null)
            {
                string debianVersion = version;
                if (!version?.Contains(".debian") ?? false)
                {
                    debianVersion = $"{version}.debian";
                }
                else
                {
                    debianVersion = version?.Replace(".debian", "");
                }

                releaseLink = componentsAvailableInSw360.Where(x => x.Name.Trim().Equals(name.Trim(), StringComparison.CurrentCultureIgnoreCase)
                && x.Version.Trim().Equals(debianVersion, StringComparison.CurrentCultureIgnoreCase)).Select(x => x.ReleaseLink).FirstOrDefault();
            }

            return releaseLink ?? string.Empty;
        }

        /// <summary>
        /// Gets Release Info From Sw360
        /// </summary>
        /// <param name="item"></param>
        /// <param name="componentsAvailableInSw360"></param>
        /// <param name="sw360Service"></param>
        /// <returns>task that returns asynchronous operation</returns>
        private static async Task<ReleasesInfo> GetReleaseInfoFromSw360(Components item, List<Components> componentsAvailableInSw360, ISW360Service sw360Service)
        {
            ReleasesInfo releasesInfo = new ReleasesInfo();

            Components componentAvailable =
                componentsAvailableInSw360.FirstOrDefault(x => x.Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase)
                && x.Version.Equals(item.Version, StringComparison.InvariantCultureIgnoreCase));

            if (componentAvailable != null)
            {
                return await sw360Service.GetReleaseDataOfComponent(componentAvailable.ReleaseLink);
            }

            return releasesInfo;
        }

        /// <summary>
        /// Log Source And Download Url Warnings
        /// </summary>
        /// <param name="component"></param>
        private static void LogSourceAndDownloadUrlWarnings(ComparisonBomData component)
        {
            bool isSourceUrlMissing = string.IsNullOrEmpty(component.SourceUrl) ||
                                      component.SourceUrl.Equals(Dataconstant.SourceUrlNotFound, StringComparison.Ordinal);

            bool isDownloadUrlMissing = string.Equals(component.DownloadUrl, Dataconstant.DownloadUrlNotFound, StringComparison.Ordinal);

            if (isSourceUrlMissing && isDownloadUrlMissing)
            {
                Logger.Warn($"   └── Source URL AND Release source Download URL are not found (source file not attached) for {component.Name}-{component.Version}");
                Logger.Debug($"LogSourceAndDownloadUrlWarnings(): Both SourceUrl and DownloadUrl not found for {component.Name}-{component.Version}");
            }
            else
            {
                if (isSourceUrlMissing)
                {
                    Logger.Warn($"   └── Source URL is not found for {component.Name}-{component.Version}");
                    Logger.Debug($"LogSourceAndDownloadUrlWarnings():SourceUrl not found for {component.Name}-{component.Version}");
                }
                else if (isDownloadUrlMissing)
                {
                    Logger.Warn($"   └── Source file is not attached,Release source Download Url is not Found for {component.Name}-{component.Version}");
                }
            }

        }

    }
}
