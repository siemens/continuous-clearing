// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
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
using LCT.Common.Model;
using LCT.Services.Interface;
using LCT.Services.Model;
using LCT.SW360PackageCreator.Interfaces;
using LCT.SW360PackageCreator.Model;
using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// ComponentCreator class
    /// </summary>
    public class ComponentCreator : IComponentCreator
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public List<ComparisonBomData> UpdatedCompareBomData { get; set; } = new List<ComparisonBomData>();
        public List<ReleaseLinked> ReleasesFoundInCbom { get; set; } = new List<ReleaseLinked>();
        public List<Components> ComponentsNotLinked { get; set; } = new List<Components>();
        private Bom bom = new Bom();
        private List<Components> ListofBomComponents { get; set; } = new List<Components>();
        public static int TotalComponentsFromPackageIdentifier { get; private set; }

        public async Task<List<ComparisonBomData>> CycloneDxBomParser(CommonAppSettings appSettings,
            ISW360Service sw360Service, ICycloneDXBomParser cycloneDXBomParser, ICreatorHelper creatorHelper)
        {
            bom = cycloneDXBomParser.ParseCycloneDXBom(appSettings.BomFilePath);
            TotalComponentsFromPackageIdentifier = bom != null ? bom.Components.Count : 0;
            ListofBomComponents = await GetListOfBomData(bom?.Components ?? new List<Component>());

            // Removing Duplicates
            ListofBomComponents = RemoveDuplicateComponents(ListofBomComponents);

            List<ComparisonBomData> comparisonBomData = await creatorHelper.SetContentsForComparisonBOM(ListofBomComponents, sw360Service);
            return comparisonBomData;
        }

        private async Task<List<Components>> GetListOfBomData(List<Component> components)
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
                else
                {
                    componentsData.DownloadUrl = Dataconstant.DownloadUrlNotFound;
                    componentsData.Name = GetPackageName(item);
                    componentsData.Group = item.Group;
                    componentsData.Version = item.Version;
                    componentsData.ComponentExternalId = item.Purl.Substring(0, item.Purl.IndexOf('@'));
                    componentsData.ReleaseExternalId = item.Purl;

                    Components component = await GetSourceUrl(componentsData.Name, componentsData.Version, componentsData.ProjectType);
                    componentsData.SourceUrl = component.SourceUrl;


                    if (componentsData.ProjectType.ToUpperInvariant() == "DEBIAN")
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
            if (componentsData.ProjectType.ToLowerInvariant() == "debian" &&
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
            else if (componentsData.ProjectType.ToLowerInvariant() == "debian")
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

            foreach (var property in package.Properties)
            {
                if (property.Name?.ToLower() == Dataconstant.Cdx_ProjectType.ToLower())
                {
                    componentsData.ProjectType = property.Value;
                }

                if (property.Name?.ToLower() == Dataconstant.Cdx_IsInternal.ToLower())
                {
                    _ = bool.TryParse(property.Value, out isInternalComponent);
                }
            }

            return isInternalComponent;
        }

        private static string GetPackageName(Component item)
        {
            if (!string.IsNullOrEmpty(item.Group) && !item.Purl.Contains(Dataconstant.MavenPackage))
            {
                return $"{item.Group}/{item.Name}";
            }
            else
            {
                return item.Name;
            }
        }

        private static async Task<Components> GetSourceUrl(string name, string version, string projectType)
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
                default:
                    break;
            }
            return componentsData;
        }

        public async Task CreateComponentInSw360(CommonAppSettings appSettings,
            ISw360CreatorService sw360CreatorService, ISW360Service sw360Service, ISw360ProjectService sw360ProjectService,
            IFileOperations fileOperations, ICreatorHelper creatorHelper, List<ComparisonBomData> parsedBomData)
        {
            string sw360Url = appSettings.SW360URL;
            string bomGenerationPath = Path.GetDirectoryName(appSettings.BomFilePath);
            Logger.Debug($"Bom Generation Path - {bomGenerationPath}");

            // create component in sw360
            if (!appSettings.IsTestMode)
            {
                await CreateComponent(creatorHelper, sw360CreatorService, parsedBomData, sw360Url, appSettings);
            }

            var manuallyLinkedReleases = await GetManuallyLinkedReleasesFromProject(appSettings, sw360ProjectService);

            var releasesFoundInCbom = ReleasesFoundInCbom.Select(x => x.ReleaseId).ToList();
            // Linking releases to the project
            await sw360CreatorService.LinkReleasesToProject(releasesFoundInCbom, manuallyLinkedReleases, appSettings.SW360ProjectID);

            // update comparison bom data
            bom = await creatorHelper.GetUpdatedComponentsDetails(ListofBomComponents, UpdatedCompareBomData, sw360Service, bom);
            fileOperations.WriteContentToFile(bom, bomGenerationPath,
                FileConstant.BomFileName, appSettings.SW360ProjectName);

            // write download url not found list into .json file
            var downloadUrlNotFoundList = creatorHelper.GetDownloadUrlNotFoundList(UpdatedCompareBomData);
            fileOperations.WriteContentToFile(downloadUrlNotFoundList, bomGenerationPath,
                FileConstant.ComponentsWithoutSrcFileName, appSettings.SW360ProjectName);

            // write Kpi Data
            CreatorKpiData kpiData = creatorHelper.GetCreatorKpiData(UpdatedCompareBomData);
            fileOperations.WriteContentToFile(kpiData, bomGenerationPath,
                FileConstant.CreatorKpiDataFileName, appSettings.SW360ProjectName);

            // write kpi info to console table 
            creatorHelper.WriteCreatorKpiDataToConsole(kpiData);

            //write download url not found list to kpi 
            creatorHelper.WriteSourceNotFoundListToConsole(UpdatedCompareBomData, appSettings);

            //write list of components which are not linked
            CommonHelper.WriteComponentsNotLinkedListInConsole(ComponentsNotLinked);

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
            }
            catch (AggregateException ex)
            {
                Logger.Debug($"CreateComponent()", ex);
            }
        }

        private static async Task<List<string>> GetManuallyLinkedReleasesFromProject(CommonAppSettings appSettings, ISw360ProjectService sw360ProjectService)
        {
            List<ReleaseLinked> alreadyLinkedReleases = await sw360ProjectService.GetAlreadyLinkedReleasesByProjectId(appSettings.SW360ProjectID);
            alreadyLinkedReleases.RemoveAll(x => string.Compare(x.Comment, Dataconstant.LinkedByCATool, StringComparison.OrdinalIgnoreCase) == 0);
            var manuallyLinkedReleaseIds = alreadyLinkedReleases.Select(x => x.ReleaseId).ToList();

            return manuallyLinkedReleaseIds;
        }

        private async Task CreateComponentAndRealease(ICreatorHelper creatorHelper,
            ISw360CreatorService sw360CreatorService, ComparisonBomData item, string sw360Url, CommonAppSettings appSettings)
        {
            Logger.Debug($"Reading Component Name - {item.Name} , version - {item.Version}");

            await CreateComponentAndReleaseWhenNotAvailable(item, sw360CreatorService, creatorHelper, appSettings);

            await CreateReleaseWhenNotAvailable(item, sw360CreatorService, creatorHelper, appSettings);

            await ComponentAndReleaseAvailable(item, sw360Url, sw360CreatorService, appSettings);
        }

        private async Task CreateComponentAndReleaseWhenNotAvailable(ComparisonBomData item,
            ISw360CreatorService sw360CreatorService, ICreatorHelper creatorHelper, CommonAppSettings appSettings)
        {
       
      
            if (item.ComponentStatus == Dataconstant.NotAvailable && item.ReleaseStatus == Dataconstant.NotAvailable)
            {
                Logger.Logger.Log(null, Level.Notice, $"Creating the Component & Release : Name - {item.Name} , version - {item.Version}", null);
                var attachmentUrlList = await creatorHelper.DownloadReleaseAttachmentSource(item);

                if (item.ReleaseExternalId.Contains(Dataconstant.DebianPackage) && !attachmentUrlList.ContainsKey("SOURCE"))
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

                item.ReleaseID = createdStatus?.ReleaseStatus?.ReleaseIdToLink ?? string.Empty;
                if (!(string.IsNullOrEmpty(item.DownloadUrl) || item.DownloadUrl.Equals(Dataconstant.DownloadUrlNotFound)))
                {
                    await TriggeringFossologyUploadAndUpdateAdditionalData(item, sw360CreatorService, appSettings);
                }


                UpdatedCompareBomData.Add(item);
            }
        }

        private static async Task TriggeringFossologyUploadAndUpdateAdditionalData(ComparisonBomData item,
            ISw360CreatorService sw360CreatorService, CommonAppSettings appSettings)
        {

            if (appSettings.EnableFossTrigger && (item.ApprovedStatus.Equals("NEW_CLEARING") || item.ApprovedStatus.Equals("Not Available")))
            {
                Logger.Logger.Log(null, Level.Notice, $"\tInitiating FOSSology process for: Release : Name - {item.Name} , version - {item.Version}", null);

                string uploadId = await TriggerFossologyProcess(item, sw360CreatorService, appSettings);
                if (string.IsNullOrEmpty(uploadId))
                {
                    item.FossologyUploadStatus = Dataconstant.NotUploaded;
                    Logger.Logger.Log(null, Level.Debug, $"\tFossology upload failed  for Release : Name - {item.Name} ," +
                        $" version - {item.Version},Fossology has a wait time so re run the pipeline", null);
                }
                else
                {
                    item.FossologyUploadStatus = Dataconstant.Uploaded;
                    item.FossologyLink = $"{appSettings.Fossologyurl}{ApiConstant.FossUploadJobUrlSuffix}{uploadId}";
                    Logger.Logger.Log(null, Level.Info, $"\tFossology upload successful for Release : Name - {item.Name} , version - {item.Version}", null);
                }
                // Updating foss url in additional data
                await sw360CreatorService.UdpateSW360ReleaseContent(new Components()
                {
                    Name = item.Name,
                    Version = item.Version,
                    UploadId = uploadId,
                    ReleaseId = item.ReleaseID
                }, appSettings.Fossologyurl);
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
                Logger.Logger.Log(null, Level.Notice, $"Creating Release : Name - {item.Name} , version - {item.Version}", null);
                var attachmentUrlList = await creatorHelper.DownloadReleaseAttachmentSource(item);

                if (item.ReleaseExternalId.Contains(Dataconstant.DebianPackage) && !attachmentUrlList.ContainsKey("SOURCE"))
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

                item.ReleaseID = releaseCreateStatus?.ReleaseIdToLink ?? string.Empty;
                if (!(string.IsNullOrEmpty(item.DownloadUrl) || item.DownloadUrl.Equals(Dataconstant.DownloadUrlNotFound)))
                {
                    await TriggeringFossologyUploadAndUpdateAdditionalData(item, sw360CreatorService, appSettings);
                }

                UpdatedCompareBomData.Add(item);
                await sw360CreatorService.UpdatePurlIdForExistingComponent(item, componentId);
            }
        }

        public static async Task<string> TriggerFossologyProcess(ComparisonBomData item,
            ISw360CreatorService sw360CreatorService, CommonAppSettings appSettings)
        {
            string uploadId = string.Empty;
            try
            {

                string sw360link = $"{item.Name}:{item.Version}:{appSettings.SW360URL}{ApiConstant.Sw360ReleaseUrlApiSuffix}" +
                    $"{item.ReleaseID}#/tab-Summary";

                FossTriggerStatus fossResult = await sw360CreatorService.TriggerFossologyProcess(item.ReleaseID, sw360link);
                if (!string.IsNullOrEmpty(fossResult?.Links?.Self?.Href))
                {
                    Logger.Debug($"{fossResult?.Content?.Message}");
                    uploadId = await CheckFossologyProcessStatus(fossResult?.Links?.Self?.Href, sw360CreatorService);
                }

            }
            catch (AggregateException ex)
            {
                Logger.Debug($"\tError in TriggerFossologyProcess--{ex}");
            }
            return uploadId;
        }

        private static async Task<string> CheckFossologyProcessStatus(string link, ISw360CreatorService sw360CreatorService)
        {
            string uploadId = string.Empty;
            try
            {
                CheckFossologyProcess fossResult = await sw360CreatorService.CheckFossologyProcessStatus(link);
                if (!string.IsNullOrEmpty(fossResult?.fossologyProcessInfo?.externalTool))
                {
                    uploadId = fossResult?.fossologyProcessInfo?.processSteps[0]?.processStepIdInTool;
                }
            }
            catch (AggregateException ex)
            {
                Logger.Debug($"\tError in TriggerFossologyProcess--{ex}");
            }
            return uploadId;
        }

        private static async Task<string> GetComponentId(ComparisonBomData item, ISw360CreatorService sw360CreatorService)
        {
            string componentId = await sw360CreatorService.GetComponentId(item.Name);

            if (string.IsNullOrEmpty(componentId))
            {
                componentId = await sw360CreatorService.GetComponentIdUsingExternalId(item.Name, item.ComponentExternalId);
            }

            return componentId;
        }

        private async Task ComponentAndReleaseAvailable(ComparisonBomData item,
            string sw360Url, ISw360CreatorService sw360CreatorService, CommonAppSettings appSettings)
        {
            if (item.ComponentStatus == Dataconstant.Available && item.ReleaseStatus == Dataconstant.Available)
            {
                Logger.Logger.Log(null, Level.Notice, $"Release exists : Name - {item.Name} , version - {item.Version}", null);
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

                UpdatedCompareBomData.Add(item);
                ReleasesInfo releasesInfo = await sw360CreatorService.GetReleaseInfo(releaseId);
                string componentId = CommonHelper.GetSubstringOfLastOccurance(releasesInfo.Links?.Sw360Component?.Href, "/");
                item.ReleaseID = releaseId;
                if (IsReleaseAttachmentExist(releasesInfo))
                {
                    await TriggeringFossologyUploadAndUpdateAdditionalData(item, sw360CreatorService, appSettings);
                }

                await sw360CreatorService.UpdatePurlIdForExistingComponent(item, componentId);
                await sw360CreatorService.UpdatePurlIdForExistingRelease(item, releaseId, releasesInfo);

            }
        }

        private static bool IsReleaseAttachmentExist(ReleasesInfo releasesInfo)
        {
            var releaseAttachments = releasesInfo?.Embedded?.Sw360attachments ?? new List<Sw360Attachments>();
            return releaseAttachments.Any(x => x.AttachmentType.Equals("SOURCE"));
        }

        private void UpdateAttachmentURLInBOm(string sw360Url, ComparisonBomData item, string releaseId)
        {
            string attachmentUrl = $"{sw360Url}{ApiConstant.Sw360ReleaseApiSuffix}/{releaseId}/{ApiConstant.Attachments}";
            Uri releaseUrl = new Uri(attachmentUrl);
            item.ReleaseAttachmentLink = releaseUrl.AbsoluteUri;
            AddReleaseIdToLink(item, releaseId);
        }

        private static string GetCreatedStatus(bool status)
        {
            return status ? Dataconstant.NewlyCreated : Dataconstant.NotCreated;
        }

        private void AddReleaseIdToLink(ComparisonBomData item, string releaseIdToLink)
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

        private List<Components> RemoveDuplicateComponents(List<Components> components)
        {
            // Removes duplicate
            bom.Components = bom.Components?.GroupBy(x => new { x.Name, x.Version }).Select(y => y.First()).ToList();
            return components.GroupBy(x => new { x.Name, x.Version }).Select(y => y.First()).ToList();
        }

    }
}
