// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// The DebianProcessor class
    /// </summary>
    public class DebianProcessor(ICycloneDXBomParser cycloneDXBomParser,ISpdxBomParser spdxBomParser) : IParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
        private static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
        private List<Component> listOfInternalComponents = new List<Component>();
        private readonly IEnvironmentHelper _environmentHelper = new EnvironmentHelper();
        #region public method

        public Bom ParsePackageFile(CommonAppSettings appSettings,ref Bom unSupportedBomList)
        {
            List<string> configFiles;
            List<DebianPackage> listofComponents = new List<DebianPackage>();
            Bom bom = new Bom();
            List<Component> listComponentForBOM;

            configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Debian,_environmentHelper);
            List<string> listOfTemplateBomfilePaths = new List<string>();
            foreach (string filepath in configFiles)
            {
                if (!filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    Logger.Debug($"ParsePackageFile():FileName: " + filepath);
                    var list = ParseCycloneDX(filepath, ref bom,appSettings);
                    listofComponents.AddRange(list);
                }
                else if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listOfTemplateBomfilePaths.Add(filepath);
                    Logger.Debug($"ParsePackageFile():Template BOM file detected: {filepath}");
                }
            }

            int initialCount = listofComponents.Count;
            int totalUnsupportedComponents = ListUnsupportedComponentsForBom.Components.Count;
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += ListUnsupportedComponentsForBom.Components.Count;
            GetDistinctComponentList(ref listofComponents);
            listComponentForBOM = FormComponentReleaseExternalID(listofComponents);
            BomCreator.bomKpiData.DuplicateComponents = initialCount - listComponentForBOM.Count;
            ListUnsupportedComponentsForBom.Components = ListUnsupportedComponentsForBom.Components.Distinct(new ComponentEqualityComparer()).ToList();
            BomCreator.bomKpiData.DuplicateComponents += totalUnsupportedComponents - ListUnsupportedComponentsForBom.Components.Count;
            bom.Components = listComponentForBOM;
            string templateFilePath = SbomTemplate.GetFilePathForTemplate(listOfTemplateBomfilePaths);
            SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, bom.Components, appSettings.ProjectType);

            bom = RemoveExcludedComponents(appSettings, bom);
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();

            if (bom.Components != null)
            {
                AddSiemensDirectProperty(ref bom);
            }
            AddSiemensDirectProperty(ref ListUnsupportedComponentsForBom);
            bom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(bom.Components, bom.Dependencies);
            ListUnsupportedComponentsForBom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(ListUnsupportedComponentsForBom.Components, ListUnsupportedComponentsForBom.Dependencies);
            unSupportedBomList.Components = ListUnsupportedComponentsForBom.Components;
            unSupportedBomList.Dependencies = ListUnsupportedComponentsForBom.Dependencies;
            return bom;
        }

        private static void AddSiemensDirectProperty(ref Bom bom)
        {
            Logger.Debug("AddSiemensDirectProperty(): Starting to add SiemensDirect property to BOM components.");
            List<string> debianDirectDependencies = [.. bom.Dependencies?.Select(x => x.Ref).ToList() ?? new List<string>()];
            var bomComponentsList = bom.Components;
            foreach (var component in bomComponentsList)
            {
                Property siemensDirect = new() { Name = Dataconstant.Cdx_SiemensDirect, Value = "false" };
                if (debianDirectDependencies.Exists(x => x.Contains(component.Name) && x.Contains(component.Version)))
                {
                    Logger.Debug($"AddSiemensDirectProperty(): Component [Name: {component.Name}, Version: {component.Version}] is a direct dependency. Setting SiemensDirect property to true.");
                    siemensDirect.Value = "true";
                }
                component.Properties ??= new List<Property>();
                bool isPropExists = component.Properties.Exists(
                    x => x.Name.Equals(Dataconstant.Cdx_SiemensDirect));
                if (!isPropExists) { component.Properties.Add(siemensDirect); }
            }

            bom.Components = bomComponentsList;
            Logger.Debug("AddSiemensDirectProperty(): Completed adding SiemensDirect property to BOM components.");
        }

        public static Bom RemoveExcludedComponents(CommonAppSettings appSettings, Bom cycloneDXBOM)
        {
            return CommonHelper.RemoveExcludedComponentsFromBom(appSettings, cycloneDXBOM, 
                noOfExcludedComponents => BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents);
        }

        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings,
                                                          IJFrogService jFrogService,
                                                          IBomHelper bomhelper)
        {
            Logger.Debug("GetJfrogRepoDetailsOfAComponent():Starting to retrieve JFrog repository details for components.\n");
            // get the  component list from Jfrog for given repo + internal repo
            string[] repoList = CommonHelper.GetRepoList(appSettings);
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(repoList, jFrogService);
            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                Component updatedComponent = UpdateComponentDetails(component, aqlResultList, appSettings, bomhelper, projectType);
                modifiedBOM.Add(updatedComponent);
            }
            LogHandlingHelper.IdentifierComponentsData(componentsForBOM, listOfInternalComponents);
            Logger.Debug("GetJfrogRepoDetailsOfAComponent():Completed retrieving JFrog repository details for components.\n");
            return modifiedBOM;
        }

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData,
            CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            Logger.Debug("IdentificationOfInternalComponents(): Starting identification of internal components.");
            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList =
                await bomhelper.GetListOfComponentsFromRepo(appSettings.Debian.Artifactory.InternalRepos, jFrogService);

            var inputIterationList = componentData.comparisonBOMData;
            
            // Use the common helper method
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(
                inputIterationList, 
                component => IsInternalDebianComponent(aqlResultList, component, bomhelper));

            // update the comparison bom data
            componentData.comparisonBOMData = processedComponents;
            componentData.internalComponents = internalComponents;
            listOfInternalComponents = internalComponents;
            Logger.Debug($"IdentificationOfInternalComponents(): identified internal components:{internalComponents.Count}.");
            Logger.Debug($"IdentificationOfInternalComponents(): Completed identification of internal components.\n");
            return componentData;
        }

        #endregion

        #region private methods
        private static Component UpdateComponentDetails(Component component, List<AqlResult> aqlResultList, CommonAppSettings appSettings, IBomHelper bomhelper, Property projectType)
        {
            string repoName = GetArtifactoryRepoName(aqlResultList, component, bomhelper, out string jfrogRepoPackageName, out string jfrogRepoPath);

            string jfrogpackageName = $"{component.Name}-{component.Version}{ApiConstant.DebianExtension}";
            var hashes = aqlResultList.FirstOrDefault(x => x.Name == jfrogpackageName);
            Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = repoName };
            Property jfrogFileNameProperty = new() { Name = Dataconstant.Cdx_Siemensfilename, Value = jfrogRepoPackageName };
            Property jfrogRepoPathProperty = new() { Name = Dataconstant.Cdx_JfrogRepoPath, Value = jfrogRepoPath };

            UpdateBomKpiData(appSettings, artifactoryrepo.Value);

            // Use common helper to set component properties and hashes
            CommonHelper.SetComponentPropertiesAndHashes(component, artifactoryrepo, projectType, jfrogFileNameProperty, jfrogRepoPathProperty, hashes);

            return component;
        }        
        private static void UpdateBomKpiData(CommonAppSettings appSettings, string repoValue)
        {
            if (repoValue == appSettings.Debian.DevDepRepo)
            {
                BomCreator.bomKpiData.DevdependencyComponents++;
            }
            else if (appSettings.Debian.Artifactory.ThirdPartyRepos != null && appSettings.Debian.Artifactory.ThirdPartyRepos.Any(repo => repo.Name == repoValue))
            {
                BomCreator.bomKpiData.ThirdPartyRepoComponents++;
            }
            else if (repoValue == appSettings.Debian.ReleaseRepo)
            {
                BomCreator.bomKpiData.ReleaseRepoComponents++;
            }
            else if (repoValue == Dataconstant.NotFoundInJFrog || string.IsNullOrEmpty(repoValue))
            {
                BomCreator.bomKpiData.UnofficialComponents++;
            }
        }
        public List<DebianPackage> ParseCycloneDX(string filePath, ref Bom bom,CommonAppSettings appSettings)
        {
            List<DebianPackage> debianPackages = new List<DebianPackage>();
            bom = ExtractDetailsForJson(filePath, ref debianPackages,appSettings);
            return debianPackages;
        }
        public static string GetArtifactoryRepoName(List<AqlResult> aqlResultList,
                                                     Component component,
                                                     IBomHelper bomHelper,
                                                     out string jfrogRepoPackageName,
                                                     out string jfrogRepoPath)
        {
            Logger.Debug($"GetArtifactoryRepoName(): Starting identify JFrog repository details retrieval for component [Name: {component.Name}, Version: {component.Version}].");
            jfrogRepoPath = Dataconstant.JfrogRepoPathNotFound;
            jfrogRepoPackageName = Dataconstant.PackageNameNotFoundInJfrog;
            string jfrogcomponentName = GetJfrogcomponentNameVersionCombined(component.Name, component.Version);
            Logger.Debug($"GetArtifactoryRepoName(): Searching for component in JFrog repository with name: {jfrogcomponentName}.");
            var aqlResults = aqlResultList.FindAll(x => x.Name.Contains(
                jfrogcomponentName, StringComparison.OrdinalIgnoreCase));

            jfrogRepoPackageName = aqlResultList.FirstOrDefault(x => x.Name.Contains(
                jfrogcomponentName, StringComparison.OrdinalIgnoreCase)
            && x.Name.Contains(".deb", StringComparison.OrdinalIgnoreCase))?.Name ?? string.Empty;
            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);
            Logger.Debug($"Repo Name for the package {jfrogcomponentName} is {repoName}");

            if (repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                string fullName = bomHelper.GetFullNameOfComponent(component);
                string fullNameVersion = GetJfrogcomponentNameVersionCombined(fullName, component.Version);
                Logger.Debug($"GetArtifactoryRepoName(): Searching for component in JFrog repository with name: {fullNameVersion}.");
                if (!fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase))
                {
                    aqlResults = aqlResultList.FindAll(x => x.Name.Contains(
                    fullNameVersion, StringComparison.OrdinalIgnoreCase));
                    jfrogRepoPackageName = aqlResultList.FirstOrDefault(x => x.Name.Contains(
                        jfrogcomponentName, StringComparison.OrdinalIgnoreCase)
                    && x.Name.Contains(".deb", StringComparison.OrdinalIgnoreCase))?.Name ?? string.Empty;
                    repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);
                }
            }

            // Forming Jfrog repo Path
            if (!repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                var aqlResult = aqlResults.FirstOrDefault(x => x.Repo.Equals(repoName));
                jfrogRepoPath = GetJfrogRepoPath(aqlResult);
                Logger.Debug($"GetJfrogArtifactoryRepoDetials(): JFrog repository path: {jfrogRepoPath}.");
            }

            if (string.IsNullOrEmpty(jfrogRepoPackageName))
            {
                jfrogRepoPackageName = Dataconstant.PackageNameNotFoundInJfrog;
            }
            return repoName;
        }

        private static string GetJfrogRepoPath(AqlResult aqlResult)
        {
            if (string.IsNullOrEmpty(aqlResult.Path) || aqlResult.Path.Equals("."))
            {
                return $"{aqlResult.Repo}/{aqlResult.Name}";
            }

            return $"{aqlResult.Repo}/{aqlResult.Path}/{aqlResult.Name}";
        }

        private static string GetJfrogcomponentNameVersionCombined(string componentName, string componentVerison)
        {
            if (componentVerison.Contains(':'))
            {
                var correctVersion = CommonHelper.GetSubstringOfLastOccurance(componentVerison, ":");
                return $"{componentName}_{correctVersion}";
            }
            return $"{componentName}_{componentVerison}";
        }

        private static bool IsInternalDebianComponent(
            List<AqlResult> aqlResultList, Component component, IBomHelper bomHelper)
        {
            string jfrogcomponentName = $"{component.Name}-{component.Version}";
            if (aqlResultList.Exists(
                x => x.Name.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.Debug($"IsInternalDebianComponent(): Component [Name: {component.Name}, Version: {component.Version}] is internal,Found in JFrog repository with full name: {jfrogcomponentName}.");
                return true;
            }

            string fullName = bomHelper.GetFullNameOfComponent(component);
            string fullNameVersion = $"{fullName}-{component.Version}";
            if (!fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase)
                && aqlResultList.Exists(
                    x => x.Name.Equals(fullNameVersion, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.Debug($"IsInternalDebianComponent(): Component [Name: {component.Name}, Version: {component.Version}] is internal,Found in JFrog repository with full name: {fullNameVersion}.");
                return true;
            }

            return false;
        }

        private Bom ExtractDetailsForJson(string filePath, ref List<DebianPackage> debianPackages,CommonAppSettings appSettings)
        {
            Bom listUnsupportedComponents = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
            Bom bom = BomHelper.ParseBomFile(filePath, _spdxBomParser, _cycloneDXBomParser,appSettings,ref listUnsupportedComponents);

            foreach (var componentsInfo in bom.Components)
            {
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                DebianPackage package = new DebianPackage
                {
                    Name = componentsInfo.Name,
                    Version = componentsInfo.Version,
                    PurlID = componentsInfo.Purl,
                    SpdxComponentDetails = new SpdxComponentInfo(),
                };
                SetSpdxComponentDetails(filePath, package,componentsInfo);

                if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.Purl) && componentsInfo.Purl.Contains(Dataconstant.PurlCheck()["DEBIAN"]))
                {
                    BomCreator.bomKpiData.DebianComponents++;
                    debianPackages.Add(package);
                }
                else
                {
                    BomCreator.bomKpiData.ComponentsExcluded++;
                    Logger.Debug($"ExtractDetailsForJson():InvalidComponent for Debian : Component Details : {package.Name} @ {package.Version} @ {package.PurlID}");
                }
            }
            ListUnsupportedComponentsForBom.Components.AddRange(listUnsupportedComponents.Components);
            ListUnsupportedComponentsForBom.Dependencies.AddRange(listUnsupportedComponents.Dependencies);
            return bom;
        }

        private static void GetDistinctComponentList(ref List<DebianPackage> listofComponents)
        {
            int initialCount = listofComponents.Count;
            listofComponents = listofComponents.GroupBy(x => new { x.Name, x.Version, x.PurlID }).Select(y => y.First()).ToList();

            if (listofComponents.Count != initialCount)
                BomCreator.bomKpiData.DuplicateComponents = initialCount - listofComponents.Count;
        }

        private static string GetReleaseExternalId(string name, string version)
        {
            return BomHelper.GetReleaseExternalId(name, version, Dataconstant.PurlCheck()["DEBIAN"]);
        }
        private static List<Component> FormComponentReleaseExternalID(List<DebianPackage> listOfComponents)
        {
            List<Component> listComponentForBOM = new List<Component>();

            foreach (var prop in listOfComponents)
            {
                string releaseExternalId = GetReleaseExternalId(prop.Name, prop.Version);
                Component component = CommonHelper.CreateComponentWithProperties(
                    prop.Name,
                    prop.Version,
                    releaseExternalId
                );
                Property isDev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };
                component.Properties = new List<Property> { isDev };
                AddComponentProperties(prop, component);
                listComponentForBOM.Add(component);
            }
            return listComponentForBOM;
        }
        private static void AddComponentProperties(DebianPackage prop, Component component)
        {
            if (prop.SpdxComponentDetails.SpdxComponent)
            {
                string fileName = Path.GetFileName(prop.SpdxComponentDetails.SpdxFilePath);
                SpdxSbomHelper.AddSpdxComponentProperties(fileName, component);
                SpdxSbomHelper.AddDevelopmentPropertyForSpdx(prop.SpdxComponentDetails.DevComponent, component);
            }
            else
            {
                //For Debian projects we will be considering CycloneDX file reading components as Discovered
                //since it's Discovered from syft Tool
                Property identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.Discovered };
                component.Properties.Add(identifierType);

            }
        }
        private static void SetSpdxComponentDetails(string filePath, DebianPackage package,Component componentInfo)
        {
            if (filePath.EndsWith(FileConstant.SPDXFileExtension))
            {
                package.SpdxComponentDetails.SpdxFilePath = filePath;
                package.SpdxComponentDetails.SpdxComponent = true;
                package.SpdxComponentDetails.DevComponent = componentInfo.Properties?.Any(x => x.Name == Dataconstant.Cdx_IsDevelopment && x.Value == "true") ?? false;
            }
        }

        #endregion
    }
}
