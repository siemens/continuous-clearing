// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.Foss;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Logging;
using LCT.Common.Model;
using LCT.Services.Interface;
using LCT.Services.Model;
using LCT.SW360PackageCreator.Interfaces;
using LCT.SW360PackageCreator.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Directory = System.IO.Directory;
using Level = log4net.Core.Level;

namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// ComponentCreator class
    /// </summary>
    public class ComponentCreator : IComponentCreator
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly CreatorKpiData s_kpiData = new();
        public static CreatorKpiData KpiData => s_kpiData;
        public List<ComparisonBomData> UpdatedCompareBomData { get; set; } = new List<ComparisonBomData>();
        public List<ReleaseLinked> ReleasesFoundInCbom { get; set; } = new List<ReleaseLinked>();
        public List<Components> ComponentsNotLinked { get; set; } = new List<Components>();
        private Bom bom = new Bom();
        private List<Components> ListofBomComponents { get; set; } = new List<Components>();
        public static int TotalComponentsFromPackageIdentifier { get; private set; }
        public async Task<List<ComparisonBomData>> CycloneDxBomParser(CommonAppSettings appSettings,
            ISW360Service sw360Service, ICycloneDXBomParser cycloneDXBomParser, ICreatorHelper creatorHelper)
        {
            var bomFilePath = Path.Combine(appSettings.Directory.OutputFolder, appSettings.SW360.ProjectName + "_" + FileConstant.BomFileName);
            bom = cycloneDXBomParser.ParseCycloneDXBom(bomFilePath);
            TotalComponentsFromPackageIdentifier = bom != null ? bom.Components.Count : 0;
            ListofBomComponents = await GetListOfBomData(bom?.Components ?? new List<Component>(), appSettings);

            // Removing Duplicates
            ListofBomComponents = RemoveDuplicateComponents(ListofBomComponents);

            List<ComparisonBomData> comparisonBomData = await creatorHelper.SetContentsForComparisonBOM(ListofBomComponents, sw360Service);
            return comparisonBomData;
        }

        private async Task<List<Components>> GetListOfBomData(List<Component> components, CommonAppSettings appSettings)
        {
            List<Components> lstOfBomDataToBeCompared = new List<Components>();

            foreach (Component item in components)
            {
                Components componentsData = new Components();

                string currName = item.Name;
                string currVersion = item.Version;

                bool isInternalComponent = GetPackageType(item, ref componentsData);

                if (isInternalComponent)
                {
                    Logger.Debug($"{item.Name}-{item.Version} found as internal component. ");
                }
                else if ((componentsData.IsDev == "true" && appSettings.SW360.IgnoreDevDependency) || componentsData.ExcludeComponent == "true")
                {
                    //do nothing
                }
                else
                {
                    componentsData.DownloadUrl = Dataconstant.DownloadUrlNotFound;
                    componentsData.Name = GetPackageName(item);
                    componentsData.Group = item.Group;
                    componentsData.Version = item.Version;
                    componentsData.ComponentExternalId = item.Purl.Substring(0, item.Purl.IndexOf('@'));
                    componentsData.ReleaseExternalId = item.Purl;

                    Components component = await GetSourceUrl(componentsData.Name, componentsData.Version, componentsData.ProjectType, item.BomRef);
                    componentsData.SourceUrl = component.SourceUrl;

                    if (componentsData.ProjectType.Equals("ALPINE", StringComparison.InvariantCultureIgnoreCase))
                    {
                        componentsData.AlpineSourceData = component.AlpineSourceData;
                    }

                    if (componentsData.ProjectType.Equals("DEBIAN", StringComparison.InvariantCultureIgnoreCase))
                    {
                        componentsData = component;
                    }
                    UpdateToLocalBomFile(componentsData, currName, currVersion);

                    lstOfBomDataToBeCompared.Add(componentsData);
                }
            }

            return lstOfBomDataToBeCompared;
        }

        private void UpdateToLocalBomFile(Components componentsData, string currName, string currVersion)
        {
            Component currBom;
            if (componentsData.ProjectType.Equals("debian", StringComparison.InvariantCultureIgnoreCase) &&
                (currName != componentsData.Name || currVersion != componentsData.Version))
            {
                Logger.Debug($"Source name found for binary package {currName}-{currVersion} --" +
                    $" Source name and version ==> {componentsData.Name}-{componentsData.Version}");

                //Update local Bom if any source or version details is changed for Debian Components
                currBom = bom.Components?.Find(val => val.Name == currName && val.Version == currVersion);

                if (currBom != null)
                {
                    currBom.Name = componentsData.Name;
                    currBom.Version = $"{componentsData.Version}.debian";
                    currBom.Purl = UrlHelper.GetReleaseExternalId(componentsData.Name, componentsData.Version);
                    currBom.BomRef = currBom.Purl;
                }

                componentsData.Version = $"{componentsData.Version}.debian";
            }
            else if (componentsData.ProjectType.Equals("debian", StringComparison.InvariantCultureIgnoreCase))
            {
                //Append .debian to all Debian type component releases
                currBom = bom.Components?.Find(val => val.Name == currName && val.Version == currVersion);
                if (currBom != null)
                {
                    currBom.Version = $"{componentsData.Version}.debian";
                }
                componentsData.Version = $"{componentsData.Version}.debian";
            }
            else
            {
                Logger.Debug($"Local Bom not updated for {currName}-{currVersion}.");
            }
        }

        private static bool GetPackageType(Component package, ref Components componentsData)
        {
            bool isInternalComponent = false;

            if (package.Properties == null)
                return isInternalComponent;

            foreach (var property in package.Properties)
            {
                if (string.Equals(property.Name, Dataconstant.Cdx_ProjectType, StringComparison.CurrentCultureIgnoreCase))
                {
                    componentsData.ProjectType = property.Value;
                }
                if (string.Equals(property.Name, Dataconstant.Cdx_IsInternal, StringComparison.CurrentCultureIgnoreCase))
                {
                    _ = bool.TryParse(property.Value, out isInternalComponent);
                }
                if (string.Equals(property.Name, Dataconstant.Cdx_IsDevelopment, StringComparison.CurrentCultureIgnoreCase))
                {
                    componentsData.IsDev = property.Value;
                }
                if (string.Equals(property.Name, Dataconstant.Cdx_ExcludeComponent, StringComparison.CurrentCultureIgnoreCase))
                {
                    componentsData.ExcludeComponent = property.Value;
                }
            }

            return isInternalComponent;
        }

        private static string GetPackageName(Component item)
        {
            if (!string.IsNullOrEmpty(item.Group) && !item.Purl.Contains(Dataconstant.PurlCheck()["MAVEN"]))
            {
                return $"{item.Group}/{item.Name}";
            }
            else
            {
                return item.Name;
            }
        }

        private static async Task<Components> GetSourceUrl(string name, string version, string projectType, string bomRef)
        {
            Components componentsData = new Components();
            switch (projectType.ToUpperInvariant())
            {
                case "NPM":
                    componentsData.SourceUrl = UrlHelper.Instance.GetSourceUrlForNpmPackage(name, version);
                    break;
                case "NUGET":
                    componentsData.SourceUrl = await UrlHelper.Instance.GetSourceUrlForNugetPackage(name, version);
                    break;
                case "DEBIAN":
                    Components debComponentData = await UrlHelper.Instance.GetSourceUrlForDebianPackage(name, version);
                    componentsData = debComponentData;
                    componentsData.ProjectType = projectType;
                    break;
                case "POETRY":
                    componentsData.SourceUrl = await UrlHelper.Instance.GetSourceUrlForPythonPackage(name, version);
                    break;
                case "CONAN":
                    componentsData.SourceUrl = await UrlHelper.Instance.GetSourceUrlForConanPackage(name, version);
                    break;
                case "ALPINE":
                    Components alpComponentData = await UrlHelper.Instance.GetSourceUrlForAlpinePackage(name, version, bomRef);
                    componentsData = alpComponentData;
                    componentsData.ProjectType = projectType;
                    break;
                default:
                    break;
            }
            return componentsData;
        }

        public async Task CreateComponentInSw360(CommonAppSettings appSettings,
            ISw360CreatorService sw360CreatorService, ISW360Service sw360Service, ISw360ProjectService sw360ProjectService,
            IFileOperations fileOperations, ICreatorHelper creatorHelper, List<ComparisonBomData> parsedBomData)
        {
            string sw360Url = appSettings.SW360.URL;
            string bomGenerationPath = appSettings.Directory.OutputFolder;
            Logger.Debug($"Bom Generation Path - {bomGenerationPath}");

            // create component in sw360
            await CreateComponent(creatorHelper, sw360CreatorService, parsedBomData, sw360Url, appSettings);
            var alreadyLinkedReleases = await GetAlreadyLinkedReleasesByProjectId(appSettings.SW360.ProjectID, sw360ProjectService);

            var manuallyLinkedReleases = await GetManuallyLinkedReleasesFromProject(alreadyLinkedReleases);

            await UpdateSBOMReleasesWithSw360Info(alreadyLinkedReleases);

            var releasesFoundInCbom = ReleasesFoundInCbom.ToList();

            // Linking releases to the project
            await sw360CreatorService.LinkReleasesToProject(releasesFoundInCbom, manuallyLinkedReleases, appSettings.SW360.ProjectID);

            // update comparison bom data
            bom = await creatorHelper.GetUpdatedComponentsDetails(ListofBomComponents, UpdatedCompareBomData, sw360Service, bom);

            var formattedString = CycloneDX.Json.Serializer.Serialize(bom);

            fileOperations.WriteContentToOutputBomFile(formattedString, bomGenerationPath,
                FileConstant.BomFileName, appSettings.SW360.ProjectName);

            // write download url not found list into .json file
            var downloadUrlNotFoundList = creatorHelper.GetDownloadUrlNotFoundList(UpdatedCompareBomData);
            fileOperations.WriteContentToFile(downloadUrlNotFoundList, bomGenerationPath,
                FileConstant.ComponentsWithoutSrcFileName, appSettings.SW360.ProjectName);

            // write Kpi Data
            var kpiData = creatorHelper.GetCreatorKpiData(UpdatedCompareBomData);
            fileOperations.WriteContentToFile(kpiData, bomGenerationPath,
                FileConstant.CreatorKpiDataFileName, appSettings.SW360.ProjectName);

            // write kpi info to console table 
            creatorHelper.WriteCreatorKpiDataToConsole(kpiData);
            UpdateKpiData(kpiData);
            //write download url not found list to kpi 
            creatorHelper.WriteSourceNotFoundListToConsole(UpdatedCompareBomData, appSettings);

            //write list of components which are not linked
            LoggerHelper.WriteComponentsNotLinkedListInConsole(ComponentsNotLinked);

            Logger.Debug($"CreateComponentInSw360():End");
        }

        private async Task CreateComponent(ICreatorHelper creatorHelper,
            ISw360CreatorService sw360CreatorService, List<ComparisonBomData> componentsToBoms,
            string sw360Url, CommonAppSettings appSettings)
        {
            Logger.Logger.Log(null, Level.Notice, $"No of Unique and Valid components read from Comparison BOM = {componentsToBoms.Count} ", null);

            try
            {
                foreach (ComparisonBomData item in componentsToBoms)
                {
                    await CreateComponentAndRealease(creatorHelper, sw360CreatorService, item, sw360Url, appSettings);
                }

                string localPathforSourceRepo = UrlHelper.GetDownloadPathForAlpineRepo();
                if (Directory.GetDirectories(localPathforSourceRepo).Length != 0)
                {
                    DirectoryInfo di = new DirectoryInfo(localPathforSourceRepo);
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        if (!dir.Name.Equals("aports"))
                        {
                            dir.Delete(true);
                        }
                    }
                }

            }
            catch (AggregateException ex)
            {
                Logger.Debug($"CreateComponent()", ex);
            }
        }

        private static async Task<List<ReleaseLinked>> GetManuallyLinkedReleasesFromProject(List<ReleaseLinked> alreadyLinkedReleases)
        {
            var manuallyLinkedReleases = new List<ReleaseLinked>(alreadyLinkedReleases);
            manuallyLinkedReleases.RemoveAll(x => string.Compare(x.Comment, Dataconstant.LinkedByCATool, StringComparison.OrdinalIgnoreCase) == 0);
            await Task.Yield();
            return manuallyLinkedReleases;
        }

        private static async Task<List<ReleaseLinked>> GetAlreadyLinkedReleasesByProjectId(string projectId, ISw360ProjectService sw360ProjectService)
        {
            List<ReleaseLinked> alreadyLinkedReleases = await sw360ProjectService.GetAlreadyLinkedReleasesByProjectId(projectId);
            return alreadyLinkedReleases;
        }

        private async Task UpdateSBOMReleasesWithSw360Info(List<ReleaseLinked> alreadyLinkedReleases)
        {
            foreach (var release in ReleasesFoundInCbom)
            {
                var linkedRelease = alreadyLinkedReleases.FirstOrDefault(r => r.ReleaseId == release.ReleaseId);
                if (linkedRelease != null)
                {
                    release.Comment = linkedRelease.Comment;
                    release.Relation = linkedRelease.Relation;
                }
            }
            await Task.Yield();
        }

        private async Task CreateComponentAndRealease(ICreatorHelper creatorHelper,
            ISw360CreatorService sw360CreatorService, ComparisonBomData item, string sw360Url, CommonAppSettings appSettings)
        {
            Logger.Debug($"Reading Component Name - {item.Name} , version - {item.Version}");

            await CreateComponentAndReleaseWhenNotAvailable(item, sw360CreatorService, creatorHelper, appSettings);

            await CreateReleaseWhenNotAvailable(item, sw360CreatorService, creatorHelper, appSettings);

            await ComponentAndReleaseAvailable(item, sw360Url, sw360CreatorService, appSettings, creatorHelper);
        }

        private async Task CreateComponentAndReleaseWhenNotAvailable(ComparisonBomData item,
            ISw360CreatorService sw360CreatorService, ICreatorHelper creatorHelper, CommonAppSettings appSettings)
        {
            if (item.ComponentStatus == Dataconstant.NotAvailable && item.ReleaseStatus == Dataconstant.NotAvailable)
            {
                LoggerHelper.WriteComponentstatusMessage("Creating the Component & Release ", item);
                var attachmentUrlList = await creatorHelper.DownloadReleaseAttachmentSource(item);

                if (item.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["DEBIAN"]) && !attachmentUrlList.ContainsKey("SOURCE"))
                {
                    item.DownloadUrl = Dataconstant.DownloadUrlNotFound;
                    UpdatedCompareBomData.Add(item);
                    return;
                }

                //till here

                ComponentCreateStatus createdStatus = await sw360CreatorService.CreateComponentBasesOFswComaprisonBOM(item, attachmentUrlList);
                item.IsComponentCreated = GetCreatedStatus(createdStatus.IsCreated);
                item.IsReleaseCreated = GetCreatedStatus(createdStatus.ReleaseStatus.IsCreated);
                item.ReleaseAttachmentLink = createdStatus.ReleaseStatus.AttachmentApiUrl;
                item.DownloadUrl = !attachmentUrlList.ContainsKey("SOURCE") ? Dataconstant.DownloadUrlNotFound : item.DownloadUrl;
                if (!string.IsNullOrEmpty(createdStatus.ReleaseStatus.ReleaseIdToLink))
                    AddReleaseIdToLink(item, createdStatus.ReleaseStatus.ReleaseIdToLink);

                item.ReleaseID = createdStatus.ReleaseStatus?.ReleaseIdToLink ?? string.Empty;
                await ProcessReleaseAlreadyExist(item, sw360CreatorService, appSettings, createdStatus.ReleaseStatus);

                UpdatedCompareBomData.Add(item);
            }
        }

        public static async Task TriggeringFossologyUploadAndUpdateAdditionalData(ComparisonBomData item,
            ISw360CreatorService sw360CreatorService, CommonAppSettings appSettings)
        {

            if (appSettings.SW360.Fossology.EnableTrigger && (item.ApprovedStatus.Equals(Dataconstant.NewClearing) || item.ApprovedStatus.Equals("Not Available") || item.ApprovedStatus.Equals(Dataconstant.SentToClearingState) || item.ApprovedStatus.Equals(Dataconstant.ScanAvailableState)))
            {
                var formattedName = GetFormattedName(item);

                bool fossologyUpload = await UpdateFossologyStatus(item, sw360CreatorService, appSettings, formattedName);

                if (!fossologyUpload)
                {
                    LoggerHelper.WriteFossologyProcessInitializeMessage(formattedName,item);                    
                    string uploadId;
                    uploadId = await TriggerFossologyProcess(item, sw360CreatorService, appSettings);

                    if (string.IsNullOrEmpty(uploadId))
                    {
                        item.FossologyUploadStatus = Dataconstant.NotUploaded;
                    }
                    else
                    {
                        await UpdateFossologyLinkAndStatus(item, sw360CreatorService, appSettings, formattedName, uploadId, "\t└── ✅ Fossology upload completed successfully for release");
                    }
                }
            }
            else
            {
                item.FossologyUploadStatus = Dataconstant.NotUploaded;
            }
        }
        private async Task CreateReleaseWhenNotAvailable(ComparisonBomData item,
            ISw360CreatorService sw360CreatorService, ICreatorHelper creatorHelper, CommonAppSettings appSettings)
        {
            if (item.ComponentStatus == Dataconstant.Available && item.ReleaseStatus == Dataconstant.NotAvailable)
            {
                LoggerHelper.WriteComponentstatusMessage("Creating Release ",item);
                var attachmentUrlList = await creatorHelper.DownloadReleaseAttachmentSource(item);

                if (item.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["DEBIAN"]) && !attachmentUrlList.ContainsKey("SOURCE"))
                {
                    item.DownloadUrl = Dataconstant.DownloadUrlNotFound;
                    UpdatedCompareBomData.Add(item);
                    return;
                }

                string componentId = await GetComponentId(item, sw360CreatorService);
                ReleaseCreateStatus releaseCreateStatus = await sw360CreatorService.CreateReleaseForComponent(item, componentId, attachmentUrlList);

                item.IsReleaseCreated = GetCreatedStatus(releaseCreateStatus.IsCreated);
                item.ReleaseAttachmentLink = releaseCreateStatus.AttachmentApiUrl;
                item.DownloadUrl = !attachmentUrlList.ContainsKey("SOURCE") ? Dataconstant.DownloadUrlNotFound : item.DownloadUrl;
                if (!string.IsNullOrEmpty(releaseCreateStatus.ReleaseIdToLink))
                    AddReleaseIdToLink(item, releaseCreateStatus.ReleaseIdToLink);

                item.ReleaseID = releaseCreateStatus.ReleaseIdToLink ?? string.Empty;
                await ProcessReleaseAlreadyExist(item, sw360CreatorService, appSettings, releaseCreateStatus);
                UpdatedCompareBomData.Add(item);
                await sw360CreatorService.UpdatePurlIdForExistingComponent(item, componentId);
            }
        }
        public static async Task<bool> UpdateFossologyStatus(ComparisonBomData item, ISw360CreatorService sw360CreatorService, CommonAppSettings appSettings, string formattedName)
        {
            bool fossologyUpload = false;
            if (!string.IsNullOrEmpty(item.FossologyLink) && !string.IsNullOrEmpty(item.FossologyUploadId))
            {
                fossologyUpload = true;
                item.FossologyUploadStatus = Dataconstant.AlreadyUploaded;
            }
            else if (!string.IsNullOrEmpty(item.FossologyUploadId) && string.IsNullOrEmpty(item.FossologyLink))
            {
                fossologyUpload = true;
                Logger.Logger.Log(null, Level.Notice, $"\tInitiating FOSSology process for: Release : Name - {formattedName} , version - {item.Version}", null);
                await UpdateFossologyLinkAndStatus(item, sw360CreatorService, appSettings, formattedName, item.FossologyUploadId, "\t🔗 Fossology upload ID and URL successfully updated in SW360 for release");
            }
            return fossologyUpload;
        }
        public static string GetFormattedName(ComparisonBomData item)
        {
            if (!string.IsNullOrEmpty(item.ParentReleaseName) && !item.ParentReleaseName.Equals(item.Name, StringComparison.OrdinalIgnoreCase))
            {
                return $"{item.ParentReleaseName}\\{item.Name}";
            }
            else
            {
                return item.Name;
            }
        }
        public static async Task<string> TriggerFossologyProcess(ComparisonBomData item,
            ISw360CreatorService sw360CreatorService, CommonAppSettings appSettings)
        {
            string uploadId = string.Empty;
            try
            {

                string sw360link = $"{item.Name}:{item.Version}:{appSettings.SW360.URL}{ApiConstant.Sw360ReleaseUrlApiSuffix}" +
                    $"{item.ReleaseID}#/tab-Summary";

                FossTriggerStatus fossResult = await sw360CreatorService.TriggerFossologyProcess(item.ReleaseID, sw360link);
                if (!string.IsNullOrEmpty(fossResult?.Links?.Self?.Href))
                {
                    Logger.Debug($"{fossResult.Content?.Message}");
                    uploadId = await CheckFossologyProcessStatus(fossResult.Links?.Self?.Href, sw360CreatorService, item);
                }

            }
            catch (AggregateException ex)
            {
                Logger.DebugFormat("\tError in TriggerFossologyProcess--{0}", ex);
            }
            return uploadId;
        }

        public static async Task<string> CheckFossologyProcessStatus(string link, ISw360CreatorService sw360CreatorService, ComparisonBomData item)
        {
            string uploadId = string.Empty;
            try
            {
                CheckFossologyProcess fossResult = await sw360CreatorService.CheckFossologyProcessStatus(link);

                if (fossResult != null)
                {
                    if (!string.IsNullOrEmpty(fossResult.FossologyProcessInfo?.ExternalTool))
                    {
                        uploadId = fossResult.FossologyProcessInfo?.ProcessSteps[0]?.ProcessStepIdInTool;
                    }
                    if (fossResult.Status == "FAILURE" && string.IsNullOrEmpty(uploadId))
                    {
                        string message = $" ❌ Fossology upload failed for release";
                        LoggerHelper.WriteFossologystatusMessage(message);
                    }
                    else if (fossResult.Status == "PROCESSING" && string.IsNullOrEmpty(uploadId))
                    {
                        string message = $" ⏳ Fossology upload is still processing. Upload ID is not yet available. Please wait and re-run the pipeline later.";
                        LoggerHelper.WriteFossologystatusMessage(message);
                    }
                }
                else
                {
                    var formattedName = GetFormattedName(item);
                    string message = $" ❌ Fossology upload failed  for Release : Name - {formattedName} , version - {item.Version}";
                    LoggerHelper.WriteFossologystatusMessage(message);
                }
            }
            catch (AggregateException ex)
            {
                Logger.DebugFormat("\tError in TriggerFossologyProcess--{0}", ex);
            }
            return uploadId;
        }

        public static async Task<string> GetComponentId(ComparisonBomData item, ISw360CreatorService sw360CreatorService)
        {
            string componentId = await sw360CreatorService.GetComponentId(item.Name);

            if (string.IsNullOrEmpty(componentId))
            {
                componentId = await sw360CreatorService.GetComponentIdUsingExternalId(item.Name, item.ComponentExternalId);
            }

            return componentId;
        }
        private static async Task<bool> UpdateFossologyLinkAndStatus(ComparisonBomData item, ISw360CreatorService sw360CreatorService, CommonAppSettings appSettings, string formattedName, string uploadId, string logPrefix)
        {
            item.FossologyLink = $"{appSettings.SW360.Fossology.URL}{ApiConstant.FossUploadJobUrlSuffix}{uploadId}";
            bool uploadStatus = await sw360CreatorService.UpdateSW360ReleaseContent(new Components
            {
                Name = item.Name,
                Version = item.Version,
                UploadId = uploadId,
                ReleaseId = item.ReleaseID,
                ReleaseCreatedBy = item.ReleaseCreatedBy,
            }, appSettings.SW360.Fossology.URL);

            if (uploadStatus)
            {
                Logger.Logger.Log(null, Level.Info, $"{logPrefix} : Name - {formattedName}, version - {item.Version}", null);
                item.FossologyUploadStatus = Dataconstant.Uploaded;
            }
            else
            {
                item.FossologyUploadStatus = Dataconstant.NotUploaded;
            }
            return uploadStatus;
        }
        private async Task ComponentAndReleaseAvailable(ComparisonBomData item,
            string sw360Url, ISw360CreatorService sw360CreatorService, CommonAppSettings appSettings, ICreatorHelper creatorHelper)
        {
            if (item.ComponentStatus == Dataconstant.Available && item.ReleaseStatus == Dataconstant.Available)
            {
                LoggerHelper.WriteComponentstatusMessage("Release exists in SW360 ", item);
                string releaseLink = item.ReleaseLink ?? string.Empty;
                string releaseId = CommonHelper.GetSubstringOfLastOccurance(releaseLink, "/");
                if (!string.IsNullOrWhiteSpace(releaseId))
                {
                    UpdateAttachmentURLInBOm(sw360Url, item, releaseId);
                }
                else
                {
                    ComponentsNotLinked.Add(new Components() { Name = item.Name, Version = item.Version });
                }

                ReleasesInfo releasesInfo = await sw360CreatorService.GetReleaseInfo(releaseId);
                string componentId = CommonHelper.GetSubstringOfLastOccurance(releasesInfo.Links?.Sw360Component?.Href, "/");
                item.ReleaseID = releaseId;
                await GetUploadIdWhenReleaseExists(item, releasesInfo, appSettings);

                // This method handles the upload of source code and updates the source code download URL for an existing release in SW360.If you don't want to upload source code just comment this method.
                await IfAlreadyReleaseExistsUploadSourceCodeAndUrlInSW360(item, releasesInfo, releaseId, creatorHelper, sw360CreatorService);
                UpdatedCompareBomData.Add(item);
                if (IsReleaseAttachmentExist(releasesInfo) && !string.IsNullOrEmpty(item.ReleaseID))
                {
                    await TriggeringFossologyUploadAndUpdateAdditionalData(item, sw360CreatorService, appSettings);
                }
                await sw360CreatorService.UpdatePurlIdForExistingComponent(item, componentId);
                await sw360CreatorService.UpdatePurlIdForExistingRelease(item, releaseId, releasesInfo);
            }
        }
        public static async Task IfAlreadyReleaseExistsUploadSourceCodeAndUrlInSW360(ComparisonBomData item, ReleasesInfo releasesInfo, string releaseId, ICreatorHelper creatorHelper, ISw360CreatorService sw360CreatorService)
        {
            if (item.ApprovedStatus == Dataconstant.NewClearing && !AreAttachmentsPresent(releasesInfo))
            {
                var attachmentUrlList = await creatorHelper.DownloadReleaseAttachmentSource(item);
                if (string.IsNullOrEmpty(releasesInfo.SourceCodeDownloadUrl))
                {
                    await sw360CreatorService.UpdateSourceCodeDownloadURLForExistingRelease(item, attachmentUrlList, releaseId);
                }
                if (attachmentUrlList != null && attachmentUrlList.Count > 0)
                {
                    string attachmentApiUrl = sw360CreatorService.AttachSourcesToReleasesCreated(releaseId, attachmentUrlList, item);
                    item.ReleaseAttachmentLink = attachmentApiUrl;
                    item.DownloadUrl = !attachmentUrlList.ContainsKey("SOURCE") ? Dataconstant.DownloadUrlNotFound : item.DownloadUrl;
                }
            }
        }
        public static Task GetUploadIdWhenReleaseExists(ComparisonBomData item, ReleasesInfo releasesInfo = null, CommonAppSettings appSettings = null)
        {
            if (releasesInfo == null)
            {
                return Task.CompletedTask;
            }

            item.ApprovedStatus = releasesInfo.ClearingState;
            item.ReleaseCreatedBy = releasesInfo.CreatedBy;
            item.SourceAttachmentStatus = IsReleaseAttachmentExist(releasesInfo);
            var uploadId = releasesInfo.ExternalToolProcesses?
                .SelectMany(process => process.ProcessSteps)
                .FirstOrDefault(step => step.StepName == "01_upload")?.ProcessStepIdInTool;

            if (releasesInfo.AdditionalData != null &&
                releasesInfo.AdditionalData.TryGetValue(ApiConstant.AdditionalDataFossologyURL, out string fossologyUrl) &&
                fossologyUrl.Contains(appSettings?.SW360?.Fossology?.URL))
            {
                item.FossologyLink = fossologyUrl;
                item.FossologyUploadId = uploadId;
            }
            else if (releasesInfo.AdditionalData == null || !releasesInfo.AdditionalData.ContainsKey(ApiConstant.AdditionalDataFossologyURL))
            {
                item.FossologyUploadId = uploadId;
            }

            item.ParentReleaseName = releasesInfo.Name;

            return Task.CompletedTask;
        }
        public static async Task ProcessReleaseAlreadyExist(ComparisonBomData item, ISw360CreatorService sw360CreatorService, CommonAppSettings appSettings, ReleaseCreateStatus releaseCreateStatus)
        {
            if (releaseCreateStatus.ReleaseAlreadyExist)
            {
                if (!string.IsNullOrEmpty(item.ReleaseID))
                {
                    ReleasesInfo releasesInfo = await sw360CreatorService.GetReleaseInfo(item.ReleaseID);
                    await GetUploadIdWhenReleaseExists(item, releasesInfo, appSettings);
                    if (IsReleaseAttachmentExist(releasesInfo))
                    {
                        await TriggeringFossologyUploadAndUpdateAdditionalData(item, sw360CreatorService, appSettings);
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(item.ReleaseID) && (!string.IsNullOrEmpty(item.DownloadUrl) || item.DownloadUrl.Equals(Dataconstant.DownloadUrlNotFound)))
                {
                    await TriggeringFossologyUploadAndUpdateAdditionalData(item, sw360CreatorService, appSettings);
                }
            }
        }
        public static bool IsReleaseAttachmentExist(ReleasesInfo releasesInfo)
        {
            var releaseAttachments = releasesInfo?.Embedded?.Sw360attachments ?? new List<Sw360Attachments>();
            return releaseAttachments.Any(x => x.AttachmentType.Equals("SOURCE"));
        }
        public static bool AreAttachmentsPresent(ReleasesInfo releasesInfo)
        {
            var attachments = releasesInfo?.Embedded?.Sw360attachments ?? new List<Sw360Attachments>();
            return attachments.Any(x => x.AttachmentType.Equals("SOURCE") || x.AttachmentType.Equals("SOURCE_SELF"));
        }
        private void UpdateAttachmentURLInBOm(string sw360Url, ComparisonBomData item, string releaseId)
        {
            string attachmentUrl = $"{sw360Url}{ApiConstant.Sw360ReleaseApiSuffix}/{releaseId}/{ApiConstant.Attachments}";
            Uri releaseUrl = new Uri(attachmentUrl);
            item.ReleaseAttachmentLink = releaseUrl.AbsoluteUri;
            AddReleaseIdToLink(item, releaseId);
        }

        public static string GetCreatedStatus(bool status)
        {
            return status ? Dataconstant.NewlyCreated : Dataconstant.NotCreated;
        }

        public void AddReleaseIdToLink(ComparisonBomData item, string releaseIdToLink)
        {
            if (!string.IsNullOrWhiteSpace(releaseIdToLink))
            {
                ReleasesFoundInCbom.Add(new ReleaseLinked() { Name = item.Name, Version = item.Version, ReleaseId = releaseIdToLink });
            }
            else
            {
                Environment.ExitCode = -1;
                Logger.Fatal($"Linking release to the project is failed. " +
                            $"Release version - {item.Version} not found under this component - {item.Name}. ");
                Logger.Error($"Linking release to the project is failed. " +
                          $"Release version - {item.Version} not found under this component - {item.Name}. ");
            }
        }

        public List<Components> RemoveDuplicateComponents(List<Components> components)
        {
            // Removes duplicate
            bom.Components = bom.Components?.GroupBy(x => new { x.Name, x.Version }).Select(y => y.First()).ToList();
            return components.GroupBy(x => new { x.Name, x.Version }).Select(y => y.First()).ToList();
        }

        /// <summary>
        /// Updates the static KPI data with the provided data
        /// </summary>
        /// <param name="kpiData">The KPI data to update with</param>
        public static void UpdateKpiData(CreatorKpiData kpiData)
        {
            if (kpiData == null) return;

            // Copy properties from the provided kpiData to the static instance
            s_kpiData.ComponentsReadFromComparisonBOM = kpiData.ComponentsReadFromComparisonBOM;
            s_kpiData.ComponentsOrReleasesCreatedNewlyInSw360 = kpiData.ComponentsOrReleasesCreatedNewlyInSw360;
            s_kpiData.ComponentsOrReleasesExistingInSw360 = kpiData.ComponentsOrReleasesExistingInSw360;
            s_kpiData.ComponentsOrReleasesNotCreatedInSw360 = kpiData.ComponentsOrReleasesNotCreatedInSw360;
            s_kpiData.ComponentsWithoutSourceDownloadUrl = kpiData.ComponentsWithoutSourceDownloadUrl;
            s_kpiData.ComponentsWithSourceDownloadUrl = kpiData.ComponentsWithSourceDownloadUrl;
            s_kpiData.ComponentsWithoutPackageUrl = kpiData.ComponentsWithoutPackageUrl;
            s_kpiData.ComponentsWithoutSourceAndPackageUrl = kpiData.ComponentsWithoutSourceAndPackageUrl;
            s_kpiData.ComponentsUploadedInFossology = kpiData.ComponentsUploadedInFossology;
            s_kpiData.ComponentsNotUploadedInFossology = kpiData.ComponentsNotUploadedInFossology;
            s_kpiData.TotalDuplicateAndInValidComponents = kpiData.TotalDuplicateAndInValidComponents;
            s_kpiData.TimeTakenByComponentCreator = kpiData.TimeTakenByComponentCreator;
        }

    }
}
