// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Model;
using LCT.Facade;
using LCT.Facade.Interfaces;
using LCT.Services;
using LCT.Services.Interface;
using LCT.SW360PackageCreator.Interfaces;
using LCT.SW360PackageCreator.Model;
using log4net;
using log4net.Core;
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

namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// CreatorHelper class
    /// </summary>
    public class CreatorHelper : ICreatorHelper
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly List<Components> lstReleaseNotCreated = new List<Components>();
        List<Components> componentsAvailableInSw360 = new List<Components>();
        private const string SOURCE = "SOURCE";
        private readonly IDictionary<string, IPackageDownloader> _packageDownloderList;

        public CreatorHelper(IDictionary<string, IPackageDownloader> packageDownloderList)
        {
            _packageDownloderList = packageDownloderList;
        }

        public List<ComparisonBomData> GetDownloadUrlNotFoundList(List<ComparisonBomData> comparisionBomDataList)
        {
            List<ComparisonBomData> downloadUrlNotFoundList = new List<ComparisonBomData>();

            foreach (ComparisonBomData comparionBomData in comparisionBomDataList)
            {
                if (comparionBomData.DownloadUrl == Dataconstant.DownloadUrlNotFound || string.IsNullOrEmpty(comparionBomData.DownloadUrl))
                {
                    comparionBomData.ReleaseID = string.IsNullOrEmpty(comparionBomData.ReleaseLink) ?
                        comparionBomData.ReleaseID : CommonHelper.GetSubstringOfLastOccurance(comparionBomData.ReleaseLink, "/");
                    downloadUrlNotFoundList.Add(comparionBomData);
                }
            }

            return downloadUrlNotFoundList;
        }

        public async Task<Dictionary<string, string>> DownloadReleaseAttachmentSource(ComparisonBomData component)
        {
            Dictionary<string, string> AttachmentUrlList = new Dictionary<string, string>();
            string localPathforDownload = GetDownloadPathForComponetType(component);
            if (!component.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["MAVEN"]))
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Logger.Debug($"DownloadReleaseAttachmentSource()-Name-{component.Name},version-{component.Version},localPathforDownload-{localPathforDownload}");

                if (string.IsNullOrEmpty(component.SourceUrl) || component.SourceUrl.Equals(Dataconstant.SourceUrlNotFound))
                    Logger.Warn($"Source URL is not Found for {component.Name}-{component.Version}");

                if (component.DownloadUrl.Equals(Dataconstant.DownloadUrlNotFound))
                {
                    Logger.Warn($"Source file is not attached,Release source Download Url is not Found for {component.Name}-{component.Version}");
                    Logger.Debug($"DownloadReleaseAttachmentSource():Source file is not attached,Release source Download Url is not Found for {component.Name}-{component.Version}");
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
            return AttachmentUrlList;
        }

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
            }
            else if (component.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["CONAN"]))
            {
                downloadPath = await GetAttachmentUrlList(component, localPathforDownload);
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

            return downloadPath;
        }

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

            if (isWindows)
            {
                p.StartInfo.FileName = Path.Combine(@"cmd.exe");
                p.StartInfo.Arguments = $"/c mvn org.apache.maven.plugins:maven-dependency-plugin:copy -Dartifact={component.Group}:{component.Name}:{component.Version}:jar:sources -DoutputDirectory={localPathforDownload}";

            }
            else
            {
                p.StartInfo.FileName = Path.Combine(@"mvn");
                p.StartInfo.Arguments = $"org.apache.maven.plugins:maven-dependency-plugin:copy -Dartifact={component.Group}:{component.Name}:{component.Version}:jar:sources -DoutputDirectory={localPathforDownload}";

            }

            var processResult = ProcessAsyncHelper.RunAsync(p.StartInfo);
            await processResult;
        }

        public static void GetAttachmentUrlListForMvn(string localPathforDownload, ComparisonBomData component,
                                                      ref Dictionary<string, string> attachmentUrlList)
        {
            localPathforDownload = $"{localPathforDownload}{component.Name}-{component.Version}-sources.jar";

            if (File.Exists(localPathforDownload))
            {
                attachmentUrlList.Add(SOURCE, localPathforDownload);
            }

        }

        private static async Task<string> GetAttachmentUrlList(ComparisonBomData component, string localPathforDownload)
        {
            string downloadPath = string.Empty;
            try
            {
                string componenetFullName = UrlHelper.GetCorrectFileExtension(component.SourceUrl);
                string downloadFilePath = $"{localPathforDownload}{componenetFullName}";
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(downloadFilePath));

                if (!string.IsNullOrEmpty(component.SourceUrl) && !component.SourceUrl.Equals(Dataconstant.SourceUrlNotFound))
                {
                    Uri uri = new Uri(component.SourceUrl);
                    downloadPath = await UrlHelper.DownloadFileAsync(uri, downloadFilePath);
                }
            }
            catch (WebException ex)
            {
                Logger.Debug($"GetAttachmentUrlListForPython :WebException :Release Name : {component.Name}@{component.Version}-PackageUrl: ,Error {ex}");
            }
            catch (UriFormatException ex)
            {
                Logger.Debug($"GetAttachmentUrlListForPython:Release Name : {component.Name}@{component.Version}: Error {ex}");
            }

            return downloadPath;
        }


        public async Task<List<ComparisonBomData>> SetContentsForComparisonBOM(List<Components> lstComponentForBOM, ISW360Service sw360Service)
        {
            Logger.Debug($"SetContentsForComparisonBOM():Start");
            List<ComparisonBomData> comparisonBomData = new List<ComparisonBomData>();
            Logger.Logger.Log(null, Level.Notice, $"Collecting comparison BOM Data...", null);
            componentsAvailableInSw360 = await sw360Service.GetAvailableReleasesInSw360(lstComponentForBOM);

            //Checking components count before getting status of individual comp details
            comparisonBomData = await GetComparisionBomItems(lstComponentForBOM, sw360Service);

            Logger.Debug($"SetContentsForComparisonBOM():End");
            return comparisonBomData;
        }

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
                    if ((string.IsNullOrEmpty(item.SourceUrl) || item.SourceUrl == Dataconstant.SourceUrlNotFound) && !string.IsNullOrEmpty(releasesInfo.SourceCodeDownloadUrl))
                    {
                        // If not able to get source details from snapshot.org, try getting source URL from SW360
                        mapper.SourceUrl = releasesInfo.SourceCodeDownloadUrl;
                        mapper.DownloadUrl = releasesInfo.SourceCodeDownloadUrl;
                    }
                    mapper.PatchURls = item.PatchURLs;
                }
                else if (!string.IsNullOrEmpty(item.ReleaseExternalId) && item.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["MAVEN"]))
                {
                    mapper.DownloadUrl = GetMavenDownloadUrl(mapper, item, releasesInfo);
                }
                else if (!string.IsNullOrEmpty(item.ReleaseExternalId) &&
                            (item.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["POETRY"]) || item.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["CONAN"]) || item.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["ALPINE"])))
                {
                    mapper.DownloadUrl = mapper.SourceUrl;
                }
                else
                {
                    mapper.DownloadUrl = GetComponentDownloadUrl(mapper, item, repo, releasesInfo);
                }
                mapper.ApprovedStatus = GetApprovedStatus(mapper.ComponentStatus, mapper.ReleaseStatus, releasesInfo);
                mapper.IsComponentCreated = GetCreatedStatus(mapper.ComponentStatus);
                mapper.IsReleaseCreated = GetCreatedStatus(mapper.ReleaseStatus);
                mapper.FossologyUploadStatus = GetFossologyUploadStatus(mapper.ApprovedStatus);
                mapper.ReleaseAttachmentLink = string.Empty;
                mapper.ReleaseLink = GetReleaseLink(componentsAvailableInSw360, item.Name, item.Version);

                Logger.Debug($"Sw360 avilability status for Name " + mapper.Name + ":" + mapper.ComponentExternalId + "=" + mapper.ComponentStatus +
                    "-Version " + mapper.Version + ":" + mapper.ReleaseExternalId + "=" + mapper.ReleaseStatus);
                comparisonBomData.Add(mapper);
            }
            return comparisonBomData;
        }

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

        public async Task<Bom> GetUpdatedComponentsDetails(List<Components> ListofBomComponents, List<ComparisonBomData> updatedCompareBomData,
            ISW360Service sw360Service, Bom bom)
        {
            //To get latest ReleaseLinks after component creation
            Logger.Logger.Log(null, Level.Debug, $"GetUpdatedComponentsDetails", null);
            componentsAvailableInSw360 = await sw360Service.GetAvailableReleasesInSw360(ListofBomComponents);

            foreach (ComparisonBomData comBom in updatedCompareBomData)
            {
                if (string.IsNullOrEmpty(comBom.ReleaseLink))
                {
                    comBom.ReleaseLink = GetReleaseLink(componentsAvailableInSw360, comBom.Name, comBom.Version);
                }

                try
                {
                    List<Property> prop = new List<Property>
                {
                    new Property { Name = Dataconstant.Cdx_ClearingState, Value = comBom.ApprovedStatus },
                     new Property { Name = Dataconstant.Cdx_ReleaseUrl, Value = comBom.ReleaseLink },
                     new Property { Name = Dataconstant.Cdx_FossologyUrl, Value = comBom.FossologyLink ?? "" }
                };

                    if (!bom.Components.Exists(x => x.BomRef.Contains(Dataconstant.PurlCheck()["MAVEN"])))
                    {
                        bom.Components.Find(com => string.IsNullOrEmpty(com.Group) ? com.Name == comBom.Name && com.Version.Contains(comBom.Version)
                        : $"{com.Group}{Dataconstant.ForwardSlash}{com.Name}" == comBom.Name && com.Version.Contains(comBom.Version))?.Properties.AddRange(prop);
                    }
                    else
                    {
                        bom.Components.Find(com => com.Name == comBom.Name && com.Version.Contains(comBom.Version))?.Properties.AddRange(prop);
                    }
                }
                catch (JsonSerializationException ex)
                {
                    Logger.Debug($"GetUpdatedComponentsDetails() For Component = {comBom.Name} : {ex}");
                }
            }
            return bom;
        }

        private static string GetDownloadPathForComponetType(ComparisonBomData component)
        {
            string localPathforDownload = string.Empty;
            try
            {
                if (component.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["DEBIAN"]))
                {
                    localPathforDownload = $"{System.IO.Directory.GetParent(System.IO.Directory.GetCurrentDirectory())}/ClearingTool/DownloadedFiles/";
                }
                else if (component.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["ALPINE"]))
                {
                    localPathforDownload = $"{System.IO.Directory.GetParent(System.IO.Directory.GetCurrentDirectory())}/ClearingTool/DownloadedFiles/";
                }
                else
                {
                    localPathforDownload = $"{Path.GetTempPath()}/ClearingTool/DownloadedFiles/";
                }
            }
            catch (IOException ex)
            {
                Logger.Error($"GetDownloadPathForComponetType() ", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"GetDownloadPathForComponetType() ", ex);
            }

            return localPathforDownload;
        }

        public CreatorKpiData GetCreatorKpiData(List<ComparisonBomData> updatedCompareBomData)
        {
            CreatorKpiData creatorKpiData = new CreatorKpiData
            {
                ComponentsReadFromComparisonBOM = updatedCompareBomData.Count,
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
                TimeSpan.FromMilliseconds(Program.CreatorStopWatch.ElapsedMilliseconds).TotalSeconds;

            return creatorKpiData;
        }

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

        private static bool IsComponentNotCreated(ComparisonBomData item)
        {
            return item.IsComponentCreated == Dataconstant.NotCreated || item.IsReleaseCreated == Dataconstant.NotCreated;
        }

        private static bool IsComponentCreated(ComparisonBomData item)
        {
            return item.IsComponentCreated == Dataconstant.Created && item.IsReleaseCreated == Dataconstant.Created;
        }

        private static bool IsComponentNewlyCreated(ComparisonBomData item)
        {
            return (item.IsComponentCreated == Dataconstant.NewlyCreated && item.IsReleaseCreated == Dataconstant.NewlyCreated)
                                || (item.IsComponentCreated == Dataconstant.Created && item.IsReleaseCreated == Dataconstant.NewlyCreated);
        }

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

        public void WriteCreatorKpiDataToConsole(CreatorKpiData creatorKpiData)
        {
            Logger.Warn("Todo: Default component type is OSS. User is expected to manually change the component type from OSS to COTS.");
            Dictionary<string, int> printList = new Dictionary<string, int>()
            {
                {CommonHelper.Convert(creatorKpiData,nameof(creatorKpiData.ComponentsReadFromComparisonBOM)),
                    creatorKpiData.ComponentsReadFromComparisonBOM },

                { CommonHelper.Convert(creatorKpiData,nameof(creatorKpiData.ComponentsOrReleasesCreatedNewlyInSw360)),
                    creatorKpiData.ComponentsOrReleasesCreatedNewlyInSw360},

                { CommonHelper.Convert(creatorKpiData,nameof(creatorKpiData.ComponentsOrReleasesExistingInSw360)),
                    creatorKpiData.ComponentsOrReleasesExistingInSw360},

                {CommonHelper.Convert(creatorKpiData, nameof(creatorKpiData.ComponentsOrReleasesNotCreatedInSw360)),
                    creatorKpiData.ComponentsOrReleasesNotCreatedInSw360},

                { CommonHelper.Convert(creatorKpiData,nameof(creatorKpiData.ComponentsWithoutSourceDownloadUrl)),
                    creatorKpiData.ComponentsWithoutSourceDownloadUrl},

                { CommonHelper.Convert(creatorKpiData,nameof(creatorKpiData.ComponentsWithSourceDownloadUrl)),
                    creatorKpiData.ComponentsWithSourceDownloadUrl},

                {CommonHelper.Convert(creatorKpiData,nameof(creatorKpiData.ComponentsWithoutPackageUrl)),
                    creatorKpiData.ComponentsWithoutPackageUrl},

                {CommonHelper.Convert(creatorKpiData,nameof(creatorKpiData.ComponentsWithoutSourceAndPackageUrl)),
                    creatorKpiData.ComponentsWithoutSourceAndPackageUrl},

                {CommonHelper.Convert(creatorKpiData,nameof(creatorKpiData.ComponentsUploadedInFossology)),
                    creatorKpiData.ComponentsUploadedInFossology},

                {CommonHelper.Convert(creatorKpiData,nameof(creatorKpiData.ComponentsNotUploadedInFossology)),
                    creatorKpiData.ComponentsNotUploadedInFossology},

                {CommonHelper.Convert(creatorKpiData,nameof(creatorKpiData.TotalDuplicateAndInValidComponents)),
                    creatorKpiData.TotalDuplicateAndInValidComponents}
            };

            Dictionary<string, double> printTimingList = new Dictionary<string, double>()
            {
                { "ComponentCreator",creatorKpiData.TimeTakenByComponentCreator }
            };

            CommonHelper.WriteToConsoleTable(printList, printTimingList);
        }

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

            CommonHelper.WriteComponentsWithoutDownloadURLToKpi(sourceNotAvailable, lstReleaseNotCreated, appSetting.SW360.URL);
        }

        private static string GetComponentAvailabilityStatus(List<Components> componentsAvailable, Components component)
        {
            return componentsAvailable.Exists(x => x.Name.ToLowerInvariant() == component.Name.ToLowerInvariant()
            || x.ComponentExternalId.ToLowerInvariant() == component.ComponentExternalId.ToLowerInvariant()) ? Dataconstant.Available : Dataconstant.NotAvailable;
        }

        private string IsReleaseAvailable(string componentName, string componentVersion, string releaseExternalId)
        {
            if (componentsAvailableInSw360.Exists(
                x => (x.Name.ToLowerInvariant() == componentName.ToLowerInvariant() && x.Version.ToLowerInvariant() == componentVersion.ToLowerInvariant())
                || x.ReleaseExternalId.ToLowerInvariant() == releaseExternalId.ToLowerInvariant()))
            {
                return Dataconstant.Available;
            }

            return Dataconstant.NotAvailable;
        }

        public static string GetComponentDownloadUrl(ComparisonBomData mapper, Components item, IRepository repo, ReleasesInfo releasesInfo)
        {
            //IF Release already exists in SW360 , tool will not update any field.
            //Hence do not need to find DOWNLOAD URL URL here and this REDUCE the exeuction time

            if (mapper.ReleaseStatus.Equals(Dataconstant.Available))
            {
                return releasesInfo?.SourceCodeDownloadUrl ?? string.Empty;
            }
            return repo.FormGitCloneUrl(mapper.SourceUrl, item.Name, item.Version);
        }

        public static string GetApprovedStatus(string componentAvailabelStatus, string releaseAvailbilityStatus, ReleasesInfo releasesInfo)
        {

            if (componentAvailabelStatus == Dataconstant.Available && releaseAvailbilityStatus == Dataconstant.Available)
            {
                return releasesInfo?.ClearingState ?? Dataconstant.NotAvailable;
            }

            return Dataconstant.NotAvailable;
        }

        public static string GetCreatedStatus(string availabilityStatus)
        {
            return availabilityStatus == Dataconstant.Available ? Dataconstant.Created : Dataconstant.NotCreated;
        }

        public static string GetFossologyUploadStatus(string ComponentApprovedStatus)
        {
            return (ComponentApprovedStatus == Dataconstant.NotAvailable ||
                     ComponentApprovedStatus == Dataconstant.NewClearing) ? Dataconstant.NotUploaded : Dataconstant.AlreadyUploaded;
        }

        public static string GetReleaseLink(List<Components> componentsAvailableInSw360, string name, string version)
        {
            string releaseLink = componentsAvailableInSw360.Where(x => x.Name.Trim().ToLower() == name.Trim().ToLower()
            && x.Version.Trim().ToLower() == version.Trim().ToLower()).Select(x => x.ReleaseLink).FirstOrDefault();

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

                releaseLink = componentsAvailableInSw360.Where(x => x.Name.Trim().ToLower() == name.Trim().ToLower()
                && x.Version.Trim().ToLower() == debianVersion).Select(x => x.ReleaseLink).FirstOrDefault();
            }

            return releaseLink ?? string.Empty;
        }

        private static async Task<ReleasesInfo> GetReleaseInfoFromSw360(Components item, List<Components> componentsAvailableInSw360, ISW360Service sw360Service)
        {
            ReleasesInfo releasesInfo = new ReleasesInfo();

            Components componentAvailable =
                componentsAvailableInSw360.FirstOrDefault(x => x.Name.ToLowerInvariant() == item.Name.ToLowerInvariant()
                && x.Version.ToLowerInvariant() == item.Version.ToLowerInvariant());

            if (componentAvailable != null)
            {
                return await sw360Service.GetReleaseDataOfComponent(componentAvailable.ReleaseLink);
            }

            return releasesInfo;
        }
    }
}
