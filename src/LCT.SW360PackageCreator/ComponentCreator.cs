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
        private const string DebianProjectType = "DEBIAN";
        private const string SourceAttachmentType = "SOURCE";

        private static readonly CreatorKpiData s_kpiData = new();
        public static CreatorKpiData KpiData => s_kpiData;
        public List<ComparisonBomData> UpdatedCompareBomData { get; set; } = new List<ComparisonBomData>();
        public List<ReleaseLinked> ReleasesFoundInCbom { get; set; } = new List<ReleaseLinked>();
        public List<Components> ComponentsNotLinked { get; set; } = new List<Components>();
        private Bom bom = new Bom();
        private List<Components> ListofBomComponents { get; set; } = new List<Components>();
        private List<Components> ListofChocoComponents { get; set; } = new List<Components>();
        public static int TotalComponentsFromPackageIdentifier { get; private set; }

        /// <summary>
        /// cycloneDxBomParser
        /// </summary>
        /// <param name="appSettings"></param>
        /// <param name="sw360Service"></param>
        /// <param name="cycloneDXBomParser"></param>
        /// <param name="creatorHelper"></param>
        /// <returns>bom data</returns>
        public async Task<List<ComparisonBomData>> CycloneDxBomParser(CommonAppSettings appSettings,
            ISW360Service sw360Service, ICycloneDXBomParser cycloneDXBomParser, ICreatorHelper creatorHelper)
        {
            var bomFilePath = Path.Combine(appSettings.Directory.OutputFolder, appSettings.SW360.ProjectName + "_" + FileConstant.BomFileName);
            Logger.DebugFormat("CycloneDxBomParser():Identified bom file with path:{0}", bomFilePath);
            bom = cycloneDXBomParser.ParseCycloneDXBom(bomFilePath);
            // Log the components in a tabular format
            LogHandlingHelper.ListOfBomFileComponents(bomFilePath, bom?.Components ?? new List<Component>());
            TotalComponentsFromPackageIdentifier = bom != null ? bom.Components.Count : 0;
            ListofBomComponents = await GetListOfBomData(bom?.Components ?? new List<Component>(), appSettings);

            // Removing Duplicates
            ListofBomComponents = RemoveDuplicateComponents(ListofBomComponents);

            List<ComparisonBomData> comparisonBomData = await creatorHelper.SetContentsForComparisonBOM(ListofBomComponents, sw360Service);
            return comparisonBomData;
        }

        /// <summary>
        /// getListOfBomData
        /// </summary>
        /// <param name="components"></param>
        /// <param name="appSettings"></param>
        /// <returns>components list</returns>
        private async Task<List<Components>> GetListOfBomData(List<Component> components, CommonAppSettings appSettings)
        {
            List<Components> lstOfBomDataToBeCompared = new List<Components>();

            foreach (Component item in components)
            {
                Components componentsData = new Components();

                string currName = item.Name;
                string currVersion = item.Version;

                bool isInternalComponent = GetPackageType(item, ref componentsData);
                if (componentsData.ProjectType.Equals("choco", StringComparison.InvariantCultureIgnoreCase))
                {
                    Logger.DebugFormat("{0}-{1} found as Choco component.", item.Name, item.Version);
                    ListofChocoComponents.Add(new Components
                    {
                        Name = item.Name,
                        Version = item.Version,
                        ProjectType = componentsData.ProjectType
                    });
                }
                else if (isInternalComponent || (componentsData.IsDev == "true" && appSettings.SW360.IgnoreDevDependency) || componentsData.ExcludeComponent == "true")
                {
                    LogSkippedComponent(item, componentsData, appSettings, isInternalComponent);
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

                    if (componentsData.ProjectType.Equals(DebianProjectType, StringComparison.InvariantCultureIgnoreCase))
                    {
                        componentsData = component;
                    }
                    UpdateToLocalBomFile(componentsData, currName, currVersion);

                    lstOfBomDataToBeCompared.Add(componentsData);
                }
            }

            return lstOfBomDataToBeCompared;
        }

        /// <summary>
        /// updateToLocalBomFile
        /// </summary>
        /// <param name="componentsData"></param>
        /// <param name="currName"></param>
        /// <param name="currVersion"></param>
        private static void LogSkippedComponent(Component item, Components componentsData, CommonAppSettings appSettings, bool isInternalComponent)
        {
            if (isInternalComponent)
            {
                Logger.DebugFormat("{0}-{1} found as internal component.", item.Name, item.Version);
                return;
            }
            if (componentsData.IsDev == "true" && appSettings.SW360.IgnoreDevDependency)
            {
                Logger.DebugFormat("{0}-{1} found as development component.", item.Name, item.Version);
                return;
            }
            if (componentsData.ExcludeComponent == "true")
            {
                Logger.DebugFormat("{0}-{1} skipped (component marked as excluded).", item.Name, item.Version);
            }
        }
        private void UpdateToLocalBomFile(Components componentsData, string currName, string currVersion)
        {
            Component currBom;
            if (componentsData.ProjectType.Equals(DebianProjectType, StringComparison.InvariantCultureIgnoreCase) &&
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
            else if (componentsData.ProjectType.Equals(DebianProjectType, StringComparison.InvariantCultureIgnoreCase))
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
                Logger.DebugFormat("UpdateToLocalBomFile():Local BoM not updated for {0}-{1}.\n", currName, currVersion);
            }
        }

        /// <summary>
        /// gets package type
        /// </summary>
        /// <param name="package"></param>
        /// <param name="componentsData"></param>
        /// <returns>boolean value</returns>
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

        /// <summary>
        /// Generates the package name for the specified component based on its group and name properties.
        /// </summary>
        /// <param name="item">The component for which to generate the package name. Must not be null. The group and name properties are
        /// used to construct the result.</param>
        /// <returns>A string representing the package name. If the component has a non-empty group and its package URL does not
        /// indicate a Maven package, returns "{group}/{name}"; otherwise, returns the component's name.</returns>
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

        /// <summary>
        /// Retrieves source URL information for a software component based on its name, version, project type, and BOM
        /// reference.
        /// </summary>        
        /// <param name="name">The name of the software component for which to retrieve the source URL.</param>
        /// <param name="version">The version of the software component. This value is used to identify the specific release.</param>
        /// <param name="projectType">The type of project or package manager associated with the component (for example, "NPM", "NUGET",
        /// "DEBIAN"). The value is case-insensitive.</param>
        /// <param name="bomRef">The Bill of Materials (BOM) reference identifier for the component. This parameter is required for certain
        /// project types, such as "ALPINE".</param>
        /// <returns>A <see cref="Components"/> object containing the source URL and related metadata for the specified
        /// component. If the project type is not recognized, the returned object may not contain source URL
        /// information.</returns>
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
                case DebianProjectType:
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
                case "CARGO":
                    componentsData.SourceUrl = await UrlHelper.Instance.GetSourceUrlForCargoPackage(name, version);
                    break;
                default:
                    break;
            }
            return componentsData;
        }

        /// <summary>
        /// Creates a new component in SW360, updates the Software Bill of Materials (SBOM) with SW360 information,
        /// links releases to the specified SW360 project, and writes output files containing BOM data, KPI metrics, and
        /// source file information.
        /// </summary>        
        /// <param name="appSettings">The application settings containing SW360 configuration, output directory paths, and project details. Must
        /// not be null.</param>
        /// <param name="sw360CreatorService">The service used to create components and link releases within SW360. Must not be null.</param>
        /// <param name="sw360Service">The service used to retrieve and update SW360 component and release information. Must not be null.</param>
        /// <param name="sw360ProjectService">The service used to interact with SW360 projects, including retrieving linked releases. Must not be null.</param>
        /// <param name="fileOperations">The file operations service used to write BOM, KPI, and source file information to output files. Must not be
        /// null.</param>
        /// <param name="creatorHelper">The helper used for component creation, KPI data generation, and console output related to the creation
        /// process. Must not be null.</param>
        /// <param name="parsedBomData">The list of BOM data items to be processed and used for component creation in SW360. Must not be null or
        /// empty.</param>
        /// <returns>A task that represents the asynchronous operation of creating and linking components in SW360, updating SBOM
        /// data, and writing output files.</returns>
        public async Task CreateComponentInSw360(CommonAppSettings appSettings,
            ISw360CreatorService sw360CreatorService, ISW360Service sw360Service, ISw360ProjectService sw360ProjectService,
            IFileOperations fileOperations, ICreatorHelper creatorHelper, List<ComparisonBomData> parsedBomData)
        {
            Logger.Debug("CreateComponentInSw360():Create component process started");
            string sw360Url = appSettings.SW360.URL;
            string bomGenerationPath = appSettings.Directory.OutputFolder;
            Logger.DebugFormat("BoM Generation Path - {0}", bomGenerationPath);

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
                FileConstant.BomFileName, appSettings.SW360.ProjectName,appSettings);

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

            // Notify user about manual steps required for Choco packages
            LoggerHelper.WriteChocoManualStepsNotification(ListofChocoComponents);

            Logger.Debug("CreateComponentInSw360():Create component process completed");
        }

        /// <summary>
        /// creates Component
        /// </summary>
        /// <param name="creatorHelper"></param>
        /// <param name="sw360CreatorService"></param>
        /// <param name="componentsToBoms"></param>
        /// <param name="sw360Url"></param>
        /// <param name="appSettings"></param>
        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task CreateComponent(ICreatorHelper creatorHelper,
            ISw360CreatorService sw360CreatorService, List<ComparisonBomData> componentsToBoms,
            string sw360Url, CommonAppSettings appSettings)
        {
            Logger.Logger.Log(null, Level.Notice, $"No of Unique and Valid components read from BoM = {componentsToBoms.Count} ", null);
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
                LogHandlingHelper.ExceptionErrorHandling("Createing Component in SW360", $"MethodName:CreateComponent()", ex, "");
            }
        }

        /// <summary>
        /// Retrieves a list of releases that have been manually linked to a project, excluding those linked by the CA
        /// Tool.
        /// </summary>
        /// <param name="alreadyLinkedReleases">A list of releases currently linked to the project. This list may include both manually linked releases and
        /// those linked by the CA Tool.</param>
        /// <returns>A list containing only the releases that were manually linked to the project. Releases linked by the CA Tool
        /// are excluded from the returned list.</returns>
        private static async Task<List<ReleaseLinked>> GetManuallyLinkedReleasesFromProject(List<ReleaseLinked> alreadyLinkedReleases)
        {
            var manuallyLinkedReleases = new List<ReleaseLinked>(alreadyLinkedReleases);
            manuallyLinkedReleases.RemoveAll(x => string.Compare(x.Comment, Dataconstant.LinkedByCATool, StringComparison.OrdinalIgnoreCase) == 0);
            await Task.Yield();
            return manuallyLinkedReleases;
        }

        /// <summary>
        /// Asynchronously retrieves the list of releases that are already linked to the specified project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project for which to retrieve linked releases. Cannot be null or empty.</param>
        /// <param name="sw360ProjectService">An implementation of the project service used to access release linkage information. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of linked releases for
        /// the specified project. The list will be empty if no releases are linked.</returns>
        private static async Task<List<ReleaseLinked>> GetAlreadyLinkedReleasesByProjectId(string projectId, ISw360ProjectService sw360ProjectService)
        {
            List<ReleaseLinked> alreadyLinkedReleases = await sw360ProjectService.GetAlreadyLinkedReleasesByProjectId(projectId);
            return alreadyLinkedReleases;
        }

        /// <summary>
        /// Updates SBOM release entries with comment and relation information from a provided list of already linked
        /// releases.
        /// </summary>
        /// <remarks>Only releases in the SBOM that have a matching release ID in the provided list will
        /// be updated. This method does not modify releases that are not found in the input list.</remarks>
        /// <param name="alreadyLinkedReleases">A list of releases that have already been linked, containing comment and relation data to be applied to
        /// matching SBOM releases. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous update operation.</returns>
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

        /// <summary>
        /// Ensures that the specified component and its release exist in the SW360 system, creating them if they are
        /// not already available.
        /// </summary>
        /// <param name="creatorHelper">A helper used to facilitate the creation of components and releases.</param>
        /// <param name="sw360CreatorService">The service used to interact with the SW360 system for component and release operations.</param>
        /// <param name="item">The BOM data representing the component and version to be checked or created.</param>
        /// <param name="sw360Url">The base URL of the SW360 system to be used for component and release operations.</param>
        /// <param name="appSettings">The application settings containing configuration values required for the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task CreateComponentAndRealease(ICreatorHelper creatorHelper,
            ISw360CreatorService sw360CreatorService, ComparisonBomData item, string sw360Url, CommonAppSettings appSettings)
        {
            Logger.DebugFormat("Reading Component Name - {0} , version - {1}", item.Name, item.Version);

            await CreateComponentAndReleaseWhenNotAvailable(item, sw360CreatorService, creatorHelper, appSettings);

            await CreateReleaseWhenNotAvailable(item, sw360CreatorService, creatorHelper, appSettings);

            await ComponentAndReleaseAvailable(item, sw360Url, sw360CreatorService, appSettings, creatorHelper);
        }

        /// <summary>
        /// creates component and release when not available
        /// </summary>
        /// <param name="item"></param>
        /// <param name="sw360CreatorService"></param>
        /// <param name="creatorHelper"></param>
        /// <param name="appSettings"></param>
        /// <returns></returns>
        private async Task CreateComponentAndReleaseWhenNotAvailable(ComparisonBomData item,
            ISw360CreatorService sw360CreatorService, ICreatorHelper creatorHelper, CommonAppSettings appSettings)
        {
            if (item.ComponentStatus == Dataconstant.NotAvailable && item.ReleaseStatus == Dataconstant.NotAvailable)
            {
                LoggerHelper.WriteComponentStatusMessage("Creating the Component & Release ", item);
                var attachmentUrlList = await creatorHelper.DownloadReleaseAttachmentSource(item);

                if (item.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["DEBIAN"]) && !attachmentUrlList.ContainsKey(SourceAttachmentType))
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
                item.DownloadUrl = !attachmentUrlList.ContainsKey(SourceAttachmentType) ? Dataconstant.DownloadUrlNotFound : item.DownloadUrl;
                if (!string.IsNullOrEmpty(createdStatus.ReleaseStatus.ReleaseIdToLink))
                    AddReleaseIdToLink(item, createdStatus.ReleaseStatus.ReleaseIdToLink);

                item.ReleaseID = createdStatus.ReleaseStatus?.ReleaseIdToLink ?? string.Empty;
                await ProcessReleaseAlreadyExist(item, sw360CreatorService, appSettings, createdStatus.ReleaseStatus);

                UpdatedCompareBomData.Add(item);
            }
        }

        /// <summary>
        /// triggering Fossology upload and update additional data
        /// </summary>
        /// <param name="item"></param>
        /// <param name="sw360CreatorService"></param>
        /// <param name="appSettings"></param>
        /// <returns>task that returns asynchronous operation</returns>
        public static async Task TriggeringFossologyUploadAndUpdateAdditionalData(ComparisonBomData item,
            ISw360CreatorService sw360CreatorService, CommonAppSettings appSettings)
        {

            if (appSettings.SW360.Fossology.EnableTrigger && (item.ApprovedStatus.Equals(Dataconstant.NewClearing) || item.ApprovedStatus.Equals("Not Available") || item.ApprovedStatus.Equals(Dataconstant.SentToClearingState) || item.ApprovedStatus.Equals(Dataconstant.ScanAvailableState)))
            {
                Logger.DebugFormat("TriggeringFossologyUploadAndUpdateAdditionalData():Required details Name-{0}, Version-{1}, ReleaseId-{2}, ApprovedStatus-{3}", item.Name, item.Version, item.ReleaseID, item.ApprovedStatus);
                var formattedName = GetFormattedName(item);

                bool fossologyUpload = await UpdateFossologyStatus(item, sw360CreatorService, appSettings, formattedName);

                if (!fossologyUpload)
                {
                    LoggerHelper.WriteFossologyProcessInitializeMessage(formattedName, item);
                    string uploadId;
                    uploadId = await TriggerFossologyProcess(item, sw360CreatorService, appSettings);

                    if (string.IsNullOrEmpty(uploadId))
                    {
                        item.FossologyUploadStatus = Dataconstant.NotUploaded;
                    }
                    else
                    {
                        await UpdateFossologyLinkAndStatus(item, sw360CreatorService, appSettings, formattedName, uploadId, "✅ Fossology upload completed successfully for release");
                    }
                }
            }
            else
            {
                item.FossologyUploadStatus = Dataconstant.NotUploaded;
            }
        }
        /// <summary>
        /// creates release when not available
        /// </summary>
        /// <param name="item"></param>
        /// <param name="sw360CreatorService"></param>
        /// <param name="creatorHelper"></param>
        /// <param name="appSettings"></param>
        /// <returns>task that returns asynchronous operation</returns>
        private async Task CreateReleaseWhenNotAvailable(ComparisonBomData item,
            ISw360CreatorService sw360CreatorService, ICreatorHelper creatorHelper, CommonAppSettings appSettings)
        {
            if (item.ComponentStatus == Dataconstant.Available && item.ReleaseStatus == Dataconstant.NotAvailable)
            {
                LoggerHelper.WriteComponentStatusMessage("Creating Release ", item);
                var attachmentUrlList = await creatorHelper.DownloadReleaseAttachmentSource(item);

                if (item.ReleaseExternalId.Contains(Dataconstant.PurlCheck()["DEBIAN"]) && !attachmentUrlList.ContainsKey(SourceAttachmentType))
                {
                    item.DownloadUrl = Dataconstant.DownloadUrlNotFound;
                    UpdatedCompareBomData.Add(item);
                    return;
                }

                string componentId = await GetComponentId(item, sw360CreatorService);
                ReleaseCreateStatus releaseCreateStatus = await sw360CreatorService.CreateReleaseForComponent(item, componentId, attachmentUrlList);

                item.IsReleaseCreated = GetCreatedStatus(releaseCreateStatus.IsCreated);
                item.ReleaseAttachmentLink = releaseCreateStatus.AttachmentApiUrl;
                item.DownloadUrl = !attachmentUrlList.ContainsKey(SourceAttachmentType) ? Dataconstant.DownloadUrlNotFound : item.DownloadUrl;
                if (!string.IsNullOrEmpty(releaseCreateStatus.ReleaseIdToLink))
                    AddReleaseIdToLink(item, releaseCreateStatus.ReleaseIdToLink);

                item.ReleaseID = releaseCreateStatus.ReleaseIdToLink ?? string.Empty;
                await ProcessReleaseAlreadyExist(item, sw360CreatorService, appSettings, releaseCreateStatus);
                UpdatedCompareBomData.Add(item);
                await sw360CreatorService.UpdatePurlIdForExistingComponent(item, componentId);
            }
        }
        /// <summary>
        /// updates fossology status
        /// </summary>
        /// <param name="item"></param>
        /// <param name="sw360CreatorService"></param>
        /// <param name="appSettings"></param>
        /// <param name="formattedName"></param>
        /// <returns>task that returns asynchronous operation</returns>
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
                LoggerHelper.WriteFossologyProcessInitializeMessage(formattedName, item);
                await UpdateFossologyLinkAndStatus(item, sw360CreatorService, appSettings, formattedName, item.FossologyUploadId, "🔗 Fossology upload ID and URL successfully updated in SW360 for release");
            }
            return fossologyUpload;
        }

        /// <summary>
        /// gets formatted name
        /// </summary>
        /// <param name="item"></param>
        /// <returns>name</returns>
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

        /// <summary>
        /// triggers fossology process
        /// </summary>
        /// <param name="item"></param>
        /// <param name="sw360CreatorService"></param>
        /// <param name="appSettings"></param>
        /// <returns>task that returns asynchronous operation</returns>
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
                LogHandlingHelper.ExceptionErrorHandling("Error in TriggerFossologyProcess", $"MethodName:TriggerFossologyProcess()", ex, "");
            }
            return uploadId;
        }

        /// <summary>
        /// checks fossology process status
        /// </summary>
        /// <param name="link"></param>
        /// <param name="sw360CreatorService"></param>
        /// <param name="item"></param>
        /// <returns>task that returns asynchronous operation</returns>
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
                        const string message = $" ❌ Fossology upload failed for release";
                        LoggerHelper.WriteFossologyStatusMessage(message);
                    }
                    else if (fossResult.Status == "PROCESSING" && string.IsNullOrEmpty(uploadId))
                    {
                        const string message = $" ⏳ Fossology upload is still processing. Upload ID is not yet available. Please wait and re-run the pipeline later.";
                        LoggerHelper.WriteFossologyStatusMessage(message);
                    }
                }
                else
                {
                    var formattedName = GetFormattedName(item);
                    string message = $" ❌ Fossology upload failed  for Release : Name - {formattedName} , version - {item.Version}";
                    LoggerHelper.WriteFossologyStatusMessage(message);
                }
            }
            catch (AggregateException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Error in CheckFossologyProcessStatus", $"MethodName:CheckFossologyProcessStatus()", ex, "");
            }
            return uploadId;
        }

        /// <summary>
        /// gets component id
        /// </summary>
        /// <param name="item"></param>
        /// <param name="sw360CreatorService"></param>
        /// <returns>task that returns asynchronous operation</returns>
        public static async Task<string> GetComponentId(ComparisonBomData item, ISw360CreatorService sw360CreatorService)
        {
            Logger.Debug("GetComponentId(): start Identifying componentId for creating release");
            string componentId = await sw360CreatorService.GetComponentId(item.Name);

            if (string.IsNullOrEmpty(componentId))
            {
                componentId = await sw360CreatorService.GetComponentIdUsingExternalId(item.Name, item.ComponentExternalId);
            }
            Logger.DebugFormat("GetComponentId(): Identified componentId for creating release is :{0}", componentId);
            return componentId;
        }

        /// <summary>
        /// updates fossology link and status
        /// </summary>
        /// <param name="item"></param>
        /// <param name="sw360CreatorService"></param>
        /// <param name="appSettings"></param>
        /// <param name="formattedName"></param>
        /// <param name="uploadId"></param>
        /// <param name="logPrefix"></param>
        /// <returns>task that returns asynchronous operation</returns>
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
                LoggerHelper.WriteFossologySucessStatusMessage(logPrefix, formattedName, item);
                item.FossologyUploadStatus = Dataconstant.Uploaded;
            }
            else
            {
                item.FossologyUploadStatus = Dataconstant.NotUploaded;
            }
            return uploadStatus;
        }

        /// <summary>
        /// component and release available
        /// </summary>
        /// <param name="item"></param>
        /// <param name="sw360Url"></param>
        /// <param name="sw360CreatorService"></param>
        /// <param name="appSettings"></param>
        /// <param name="creatorHelper"></param>
        /// <returns>task that returns asynchronous operation</returns>
        private async Task ComponentAndReleaseAvailable(ComparisonBomData item,
            string sw360Url, ISw360CreatorService sw360CreatorService, CommonAppSettings appSettings, ICreatorHelper creatorHelper)
        {
            if (item.ComponentStatus == Dataconstant.Available && item.ReleaseStatus == Dataconstant.Available)
            {
                LoggerHelper.WriteComponentStatusMessage("Release exists in SW360 ", item);
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

        /// <summary>
        /// if already release exists upload source code and url in SW360
        /// </summary>
        /// <param name="item"></param>
        /// <param name="releasesInfo"></param>
        /// <param name="releaseId"></param>
        /// <param name="creatorHelper"></param>
        /// <param name="sw360CreatorService"></param>
        /// <returns>task that returns asynchronous operation</returns>
        public static async Task IfAlreadyReleaseExistsUploadSourceCodeAndUrlInSW360(ComparisonBomData item, ReleasesInfo releasesInfo, string releaseId, ICreatorHelper creatorHelper, ISw360CreatorService sw360CreatorService)
        {
            if (item.ApprovedStatus == Dataconstant.NewClearing && !AreAttachmentsPresent(releasesInfo))
            {
                var attachmentUrlList = await creatorHelper.DownloadReleaseAttachmentSource(item);

                if (attachmentUrlList != null && attachmentUrlList.Count > 0)
                {
                    if (string.IsNullOrEmpty(releasesInfo.SourceCodeDownloadUrl))
                    {
                        await sw360CreatorService.UpdateSourceCodeDownloadURLForExistingRelease(item, attachmentUrlList, releaseId);
                    }
                    string attachmentApiUrl = sw360CreatorService.AttachSourcesToReleasesCreated(releaseId, attachmentUrlList, item);
                    item.ReleaseAttachmentLink = attachmentApiUrl;
                    item.DownloadUrl = !attachmentUrlList.ContainsKey(SourceAttachmentType) ? Dataconstant.DownloadUrlNotFound : item.DownloadUrl;

                }
                else
                {
                    item.DownloadUrl = Dataconstant.DownloadUrlNotFound;
                }

            }
        }

        /// <summary>
        /// gets upload id when release exists
        /// </summary>
        /// <param name="item"></param>
        /// <param name="releasesInfo"></param>
        /// <param name="appSettings"></param>
        /// <returns>task that returns asynchronous operation</returns>
        public static Task GetUploadIdWhenReleaseExists(ComparisonBomData item, ReleasesInfo releasesInfo = null, CommonAppSettings appSettings = null)
        {
            if (releasesInfo == null)
            {
                Logger.Debug("GetUploadIdWhenReleaseExists(): releasesInformation is null.");
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
                Logger.DebugFormat("GetUploadIdWhenReleaseExists(): FossologyLink identified from releasedata: {0}", item.FossologyLink);
            }
            else if (releasesInfo.AdditionalData == null || !releasesInfo.AdditionalData.ContainsKey(ApiConstant.AdditionalDataFossologyURL))
            {
                item.FossologyUploadId = uploadId;
            }

            item.ParentReleaseName = releasesInfo.Name;

            return Task.CompletedTask;
        }

        /// <summary>
        /// process release already exist
        /// </summary>
        /// <param name="item"></param>
        /// <param name="sw360CreatorService"></param>
        /// <param name="appSettings"></param>
        /// <param name="releaseCreateStatus"></param>
        /// <returns>task that returns asynchronous operation</returns>
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
                if (!string.IsNullOrEmpty(item.ReleaseID) && !string.IsNullOrEmpty(item.DownloadUrl) && item.DownloadUrl != Dataconstant.DownloadUrlNotFound)
                {
                    await TriggeringFossologyUploadAndUpdateAdditionalData(item, sw360CreatorService, appSettings);
                }
            }
        }

        /// <summary>
        /// is release attachment exist
        /// </summary>
        /// <param name="releasesInfo"></param>
        /// <returns>boolean value</returns>
        public static bool IsReleaseAttachmentExist(ReleasesInfo releasesInfo)
        {
            var releaseAttachments = releasesInfo?.Embedded?.Sw360attachments ?? new List<Sw360Attachments>();
            return releaseAttachments.Any(x => x.AttachmentType.Equals(SourceAttachmentType));
        }

        /// <summary>
        /// are attachments present
        /// </summary>
        /// <param name="releasesInfo"></param>
        /// <returns>boolean value</returns>
        public static bool AreAttachmentsPresent(ReleasesInfo releasesInfo)
        {
            var attachments = releasesInfo?.Embedded?.Sw360attachments ?? new List<Sw360Attachments>();
            return attachments.Any(x => x.AttachmentType.Equals(SourceAttachmentType) || x.AttachmentType.Equals("SOURCE_SELF"));
        }

        /// <summary>
        /// updates the attachment URL in the BOM for the specified release
        /// </summary>
        /// <param name="sw360Url"></param>
        /// <param name="item"></param>
        /// <param name="releaseId"></param>
        private void UpdateAttachmentURLInBOm(string sw360Url, ComparisonBomData item, string releaseId)
        {
            string attachmentUrl = $"{sw360Url}{ApiConstant.Sw360ReleaseApiSuffix}/{releaseId}/{ApiConstant.Attachments}";
            Uri releaseUrl = new Uri(attachmentUrl);
            item.ReleaseAttachmentLink = releaseUrl.AbsoluteUri;
            AddReleaseIdToLink(item, releaseId);
        }

        /// <summary>
        /// gets created status
        /// </summary>
        /// <param name="status"></param>
        /// <returns>status</returns>
        public static string GetCreatedStatus(bool status)
        {
            return status ? Dataconstant.NewlyCreated : Dataconstant.NotCreated;
        }

        /// <summary>
        /// adds release id to link
        /// </summary>
        /// <param name="item"></param>
        /// <param name="releaseIdToLink"></param>
        public void AddReleaseIdToLink(ComparisonBomData item, string releaseIdToLink)
        {
            if (!string.IsNullOrWhiteSpace(releaseIdToLink))
            {
                ReleasesFoundInCbom.Add(new ReleaseLinked() { Name = item.Name, Version = item.Version, ReleaseId = releaseIdToLink });
            }
            else
            {
                Environment.ExitCode = -1;
                Logger.ErrorFormat("Linking release to the project is failed. Release version - {0} not found under this component - {1}. ", item.Version, item.Name);
            }
        }

        /// <summary>
        /// removes duplicate components
        /// </summary>
        /// <param name="components"></param>
        /// <returns>list of components</returns>
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
