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
using Tommy;
using Component = CycloneDX.Models.Component;
using Dependency = CycloneDX.Models.Dependency;

namespace LCT.PackageIdentifier
{
    public class PythonProcessor(ICycloneDXBomParser cycloneDXBomParser, ISpdxBomParser spdxBomParser) : IParser
    {
        private const string NotFoundInRepo = "Not Found in JFrogRepo";

        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        private static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };

        /// <summary>
        ///Parses PackageFile
        /// </summary>
        /// <param name="appSettings"></param>
        /// <param name="unSupportedBomList"></param>
        /// <returns>updated BOM data</returns>
        private List<Component> listOfInternalComponents = new List<Component>();
        private readonly IEnvironmentHelper environmentHelper = new EnvironmentHelper();
        public Bom ParsePackageFile(CommonAppSettings appSettings, ref Bom unSupportedBomList)
        {
            Logger.Debug("ParsePackageFile():Starting to parse package files for BOM.");
            List<string> configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Poetry,environmentHelper);
            List<PythonPackage> listofComponents = new List<PythonPackage>();
            Bom bom = new Bom();
            List<Component> listComponentForBOM=new List<Component>();
            List<Dependency> dependencies = new List<Dependency>();
            List<string> listOfTemplateBomfilePaths = new List<string>();
            List<Component> ListofComponentsFromLockFile = new List<Component>();
            List<Dependency> ListofDependenciesFromLockFile = new List<Dependency>();
            Bom cdxGenBomData = GetCdxGenBomData(configFiles, appSettings);
            foreach (string config in configFiles)
            {
                if (config.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listOfTemplateBomfilePaths.Add(config);
                    Logger.DebugFormat("ParsePackageFile():Template BOM file detected: {0}", config);
                }
                if (config.ToLower().EndsWith("poetry.lock"))
                {
                    Logger.DebugFormat("ParsePackageFile():Poetry lock file detected: {0}", config);
                    listofComponents=ExtractDetailsForPoetryLockfile(config, ListofDependenciesFromLockFile);
                    ListofComponentsFromLockFile.AddRange(FormComponentReleaseExternalID(listofComponents));

                }
                else if ((config.EndsWith(FileConstant.CycloneDXFileExtension) || config.EndsWith(FileConstant.DependencyFileExtension) || config.EndsWith(FileConstant.SPDXFileExtension))
         && !config.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listofComponents=ExtractDetailsFromJson(config, appSettings, ref dependencies);
                    listComponentForBOM.AddRange(FormComponentReleaseExternalID(listofComponents));
                }
            }

            CommonHelper.EnrichCdxGenforPackagefilesData(
                ref ListofComponentsFromLockFile,
                ref ListofDependenciesFromLockFile,
                ref listComponentForBOM,
                ref dependencies,
                cdxGenBomData);

            int initialCount = listComponentForBOM.Count;
            int totalUnsupportedComponents = ListUnsupportedComponentsForBom.Components.Count;
            listComponentForBOM = listComponentForBOM.Distinct(new ComponentEqualityComparer()).ToList();
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += ListUnsupportedComponentsForBom.Components.Count;
            ListUnsupportedComponentsForBom.Components = ListUnsupportedComponentsForBom.Components.Distinct(new ComponentEqualityComparer()).ToList();
            BomCreator.bomKpiData.DuplicateComponents = initialCount - listComponentForBOM.Count;
            BomCreator.bomKpiData.ComponentsInComparisonBOM = listComponentForBOM.Count;
            BomCreator.bomKpiData.DuplicateComponents += totalUnsupportedComponents - ListUnsupportedComponentsForBom.Components.Count;
            bom.Components = listComponentForBOM;
            bom.Dependencies = dependencies;
            string templateFilePath = SbomTemplate.GetFilePathForTemplate(listOfTemplateBomfilePaths);
            SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, bom.Components, appSettings.ProjectType);
            bom = RemoveExcludedComponents(appSettings, bom);
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();

            if (bom.Components != null)
            {
                AddSiemensDirectProperty(ref bom);
            }
            AddSiemensDirectProperty(ref ListUnsupportedComponentsForBom);
            unSupportedBomList.Components = ListUnsupportedComponentsForBom.Components;
            bom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(bom.Components, bom.Dependencies);
            ListUnsupportedComponentsForBom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(ListUnsupportedComponentsForBom.Components, ListUnsupportedComponentsForBom.Dependencies);
            unSupportedBomList.Dependencies = ListUnsupportedComponentsForBom.Dependencies;
            return bom;
        }

        private Bom GetCdxGenBomData(List<string> configFiles, CommonAppSettings appSettings)
        {
            return CommonIdentiferHelper.GetCdxGenBomData(configFiles, appSettings, _cycloneDXBomParser.ParseCycloneDXBom);
        }


        /// <summary>
        /// Adds Siemens DirectProperty
        /// </summary>
        /// <param name="bom"></param>
        public static void AddSiemensDirectProperty(ref Bom bom)
        {
            Logger.Debug("AddSiemensDirectProperty(): Starting to identifying Direct dependencies.");
            List<string> pythonDirectDependencies = new List<string>();
            pythonDirectDependencies.AddRange(bom.Dependencies?.Select(x => x.Ref).ToList() ?? new List<string>());
            var bomComponentsList = bom.Components;
            foreach (var component in bomComponentsList)
            {
                Property siemensDirect = new() { Name = Dataconstant.Cdx_SiemensDirect, Value = "false" };
                if (pythonDirectDependencies.Exists(x => x.Contains(component.Name) && x.Contains(component.Version)))
                {
                    Logger.DebugFormat("AddSiemensDirectProperty(): Component [Name: {0}, Version: {1}] is a direct dependency. Setting Siemens Direct property to true.", component.Name, component.Version);
                    siemensDirect.Value = "true";
                }

                component.Properties ??= new List<Property>();
                bool isPropExists = component.Properties.Exists(
                    x => x.Name.Equals(Dataconstant.Cdx_SiemensDirect));

                if (!isPropExists) { component.Properties.Add(siemensDirect); }
            }
            Logger.Debug("AddSiemensDirectProperty(): Completed identifying Direct dependencies.");
            bom.Components = bomComponentsList;
        }

        #region Private Methods

        /// <summary>
        /// Extract Details For Poetry Lock file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="dependencies"></param>
        /// <returns> list of package</returns>
        public static List<PythonPackage> ExtractDetailsForPoetryLockfile(string filePath, List<Dependency> dependencies)
        {
            List<PythonPackage> PythonPackages;
            PythonPackages = GetPackagesFromTOMLFile(filePath, dependencies);
            IdentifiedPythonPackages(filePath, PythonPackages);
            return PythonPackages;
        }

        /// <summary>
        /// Gets Packages From TOML File
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="dependencies"></param>
        /// <returns>list of package</returns>
        private static List<PythonPackage> GetPackagesFromTOMLFile(string filePath, List<Dependency> dependencies)
        {
            List<PythonPackage> PythonPackages = new();
            List<KeyValuePair<string, TomlNode>> keyValuePair = new();
            FileParser fileParser = new();
            TomlTable tomlTable = fileParser.ParseTomlFile(filePath);
            CommonHelper.WarnIfDependencyFileRequired();
            foreach (TomlNode node in tomlTable["package"])
            {
                PythonPackage pythonPackage = new()
                {
                    Name = node["name"].ToString(),
                    Version = node["version"].ToString(),
                    PurlID = Dataconstant.PurlCheck()["POETRY"] + "/" + node["name"].ToString() + "@" + node["version"].ToString(),
                    // By Default Tommy.TomlLazy is coming instead of Null or empty
                    Isdevdependent = (node["category"].ToString() != "main" && node["category"].ToString() != "Tommy.TomlLazy"),
                    FoundType = Dataconstant.Discovered,
                    SpdxComponentDetails = new SpdxComponentInfo()
                };

                if (pythonPackage.Isdevdependent)
                    BomCreator.bomKpiData.DevDependentComponents++;

                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                PythonPackages.Add(pythonPackage);
                keyValuePair.Add(new KeyValuePair<string, TomlNode>(pythonPackage.PurlID, node));
            }

            GetRefDetailsFromDependencyText(keyValuePair, dependencies, PythonPackages);

            return PythonPackages;
        }

        /// <summary>
        /// Gets Ref Details From DependencyText
        /// </summary>
        /// <param name="keyValues"></param>
        /// <param name="dependencies"></param>
        /// <param name="PythonPackages"></param>
        private static void GetRefDetailsFromDependencyText(List<KeyValuePair<string, TomlNode>> keyValues, List<Dependency> dependencies, List<PythonPackage> PythonPackages)
        {
            foreach (var node in keyValues)
            {
                var dep = node.Value["dependencies"];
                List<Dependency> subDependencies = new();
                if (dep != null && dep.ChildrenCount > 0)
                {
                    foreach (var dependency in dep.AsTable.RawTable)
                    {
                        subDependencies.Add(new Dependency()
                        {
                            Ref = FormRefFromNodeDetails(dependency, PythonPackages)
                        });
                    }
                }

                dependencies.Add(new Dependency()
                {
                    Ref = node.Key,
                    Dependencies = subDependencies
                });
            }
        }

        /// <summary>
        /// Form Ref From Node Details
        /// </summary>
        /// <param name="valuePair"></param>
        /// <param name="PythonPackages"></param>
        /// <returns>node details</returns>
        private static string FormRefFromNodeDetails(KeyValuePair<string, TomlNode> valuePair, List<PythonPackage> PythonPackages)
        {
            var value = PythonPackages.Find(val => val.Name == valuePair.Key)?.Version;

            if (string.IsNullOrEmpty(value))
            {
                return Dataconstant.PurlCheck()["POETRY"] + "/" + valuePair.Key + "@" + "*";
            }
            else
            {
                return Dataconstant.PurlCheck()["POETRY"] + "/" + valuePair.Key + "@" + value;
            }
        }

        /// <summary>
        /// Extract Details From Json
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="appSettings"></param>
        /// <param name="dependencies"></param>
        /// <returns>list of package</returns>
        private List<PythonPackage> ExtractDetailsFromJson(string filePath, CommonAppSettings appSettings, ref List<Dependency> dependencies)
        {
            Bom bom;
            List<PythonPackage> PythonPackages = new List<PythonPackage>();
            if (filePath.EndsWith(FileConstant.SPDXFileExtension))
            {
                Logger.DebugFormat("ExtractDetailsFromJson():Spdx file detected: {0}", filePath);
                BomHelper.NamingConventionOfSPDXFile(filePath, appSettings);
                Bom listUnsupportedComponents = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
                bom = _spdxBomParser.ParseSPDXBom(filePath);
                SpdxSbomHelper.CheckValidComponentsFromSpdxfile(bom, appSettings.ProjectType, ref listUnsupportedComponents);
                SpdxSbomHelper.AddSpdxSBomFileNameProperty(ref bom, filePath);
                SpdxSbomHelper.AddSpdxPropertysForUnsupportedComponents(listUnsupportedComponents.Components, filePath);
                ListUnsupportedComponentsForBom.Components.AddRange(listUnsupportedComponents.Components);
                ListUnsupportedComponentsForBom.Dependencies.AddRange(listUnsupportedComponents.Dependencies);
                LogHandlingHelper.IdentifierInputFileComponents(filePath, listUnsupportedComponents.Components);
            }
            else
            {                
                bom = _cycloneDXBomParser.ParseCycloneDXBom(filePath);
                CycloneDXBomParser.CheckValidComponentsForProjectType(bom.Components, appSettings.ProjectType);
            }

            foreach (var componentsInfo in bom.Components)
            {
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                PythonPackage package = new PythonPackage
                {
                    Name = componentsInfo.Name,
                    Version = componentsInfo.Version,
                    PurlID = componentsInfo.Purl,
                    SpdxComponentDetails = new SpdxComponentInfo(),
                };
                SetSpdxComponentDetails(filePath, package, componentsInfo);

                if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.Purl) && componentsInfo.Purl.Contains(Dataconstant.PurlCheck()["POETRY"]))
                {
                    BomCreator.bomKpiData.DebianComponents++;
                    PythonPackages.Add(package);
                    Logger.DebugFormat("ExtractDetailsFromJson():ValidComponent : Component Details : {0} @ {1} @ {2}", package.Name, package.Version, package.PurlID);
                }
                else
                {
                    BomCreator.bomKpiData.ComponentsExcluded++;
                    Logger.DebugFormat("ExtractDetailsFromJson():InvalidComponent : Component Details : {0} @ {1} @ {2}", package.Name, package.Version, package.PurlID);
                }
            }

            if (bom.Dependencies != null)
            {
                dependencies.AddRange(bom.Dependencies);
            }
            IdentifiedPythonPackages(filePath, PythonPackages);
            return PythonPackages;
        }

        /// <summary>
        /// Gets Distinct ComponentList
        /// </summary>
        /// <param name="listofComponents"></param>
        private static void GetDistinctComponentList(ref List<PythonPackage> listofComponents)
        {
            int initialCount = listofComponents.Count;
            listofComponents = listofComponents.GroupBy(x => new { x.Name, x.Version, x.PurlID }).Select(y => y.First()).ToList();

            if (listofComponents.Count != initialCount)
                BomCreator.bomKpiData.DuplicateComponents = initialCount - listofComponents.Count;
        }

        /// <summary>
        /// Gets Release ExternalId
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns>release id</returns>
        private static string GetReleaseExternalId(string name, string version)
        {
            version = WebUtility.UrlEncode(version);
            version = version.Replace("%3A", ":");

            return $"{Dataconstant.PurlCheck()["POETRY"]}{Dataconstant.ForwardSlash}{name}@{version}";
        }

        /// <summary>
        /// Form Component Release ExternalID
        /// </summary>
        /// <param name="listOfComponents"></param>
        /// <returns>list of components</returns>
        private static List<Component> FormComponentReleaseExternalID(List<PythonPackage> listOfComponents)
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
                AddComponentProperties(prop, component);
                listComponentForBOM.Add(component);
            }
            return listComponentForBOM;
        }

        /// <summary>
        /// Remove Excluded Components
        /// </summary>
        /// <param name="appSettings"></param>
        /// <param name="cycloneDXBOM"></param>
        /// <returns>updated BOM file</returns>
        private static Bom RemoveExcludedComponents(CommonAppSettings appSettings,
            Bom cycloneDXBOM)
        {
            return CommonHelper.RemoveExcludedComponentsFromBom(appSettings, cycloneDXBOM,
                noOfExcludedComponents => BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents);
        }

        /// <summary>
        /// Identification Of InternalComponents
        /// </summary>
        /// <param name="componentData"></param>
        /// <param name="appSettings"></param>
        /// <param name="jFrogService"></param>
        /// <param name="bomhelper"></param>
        /// <returns>component identification</returns>
        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {

            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList =
                await bomhelper.GetPypiListOfComponentsFromRepo(appSettings.Poetry.Artifactory.InternalRepos, jFrogService);

            var inputIterationList = componentData.comparisonBOMData;

            // Use the common helper method
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(
                inputIterationList,
                component => IsInternalPythonComponent(aqlResultList, component, bomhelper));

            // update the comparison bom data
            componentData.comparisonBOMData = processedComponents;
            componentData.internalComponents = internalComponents;
            listOfInternalComponents = internalComponents;
            return componentData;
        }

        /// <summary>
        /// Is Internal PythonComponent
        /// </summary>
        /// <param name="aqlResultList"></param>
        /// <param name="component"></param>
        /// <param name="bomHelper"></param>
        /// <returns>boolean value</returns>
        private static bool IsInternalPythonComponent(List<AqlResult> aqlResultList, Component component, IBomHelper bomHelper)
        {
            string jfrogcomponentName = bomHelper.GetFullNameOfComponent(component);
            if (aqlResultList.Exists(x => x.Properties.Any(p => p.Key == "pypi.normalized.name" && p.Value == jfrogcomponentName) && x.Properties.Any(p => p.Key == "pypi.version" && p.Value == component.Version)))
            {
                Logger.DebugFormat("IsInternalPythonComponent(): Component [Name: {0}, Version: {1}] is internal,Found in JFrog repository with full name: {2}.", component.Name, component.Version, jfrogcomponentName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets Jfrog Name Of PypiComponent
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="aqlResultList"></param>
        /// <returns>component name</returns>
        private static string GetJfrogNameOfPypiComponent(string name, string version, List<AqlResult> aqlResultList)
        {


            string nameVerison = string.Empty;
            nameVerison = aqlResultList.FirstOrDefault(x => x.Properties.Any(p => p.Key == "pypi.normalized.name" && p.Value == name) && x.Properties.Any(p => p.Key == "pypi.version" && p.Value == version))?.Name ?? string.Empty;

            if (string.IsNullOrEmpty(nameVerison)) { nameVerison = Dataconstant.PackageNameNotFoundInJfrog; }
            return nameVerison;
        }

        /// <summary>
        ///Gets Jfrog Repo Details OfAComponent
        /// </summary>
        /// <param name="componentsForBOM"></param>
        /// <param name="appSettings"></param>
        /// <param name="jFrogService"></param>
        /// <param name="bomhelper"></param>
        /// <returns>list of component</returns>
        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            // get the  component list from Jfrog for given repo + internal repo
            string[] repoList = CommonHelper.GetRepoList(appSettings);
            List<AqlResult> aqlResultList = await bomhelper.GetPypiListOfComponentsFromRepo(repoList, jFrogService);
            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                var processedComponent = ProcessPythonComponent(component, aqlResultList, bomhelper, appSettings, projectType);
                modifiedBOM.Add(processedComponent);
            }
            LogHandlingHelper.IdentifierComponentsData(componentsForBOM, listOfInternalComponents);
            return modifiedBOM;
        }

        /// <summary>
        /// Process Python Component
        /// </summary>
        /// <param name="component"></param>
        /// <param name="aqlResultList"></param>
        /// <param name="bomhelper"></param>
        /// <param name="appSettings"></param>
        /// <param name="projectType"></param>
        /// <returns>component name</returns>
        private static Component ProcessPythonComponent(Component component, List<AqlResult> aqlResultList, IBomHelper bomhelper, CommonAppSettings appSettings, Property projectType)
        {
            string repoName = GetArtifactoryRepoName(aqlResultList, component, bomhelper, out string jfrogPackageNameWhlExten, out string jfrogRepoPath);

            var hashes = aqlResultList.FirstOrDefault(x => x.Properties.Any(p => p.Key == "pypi.normalized.name" && p.Value == component.Name) && x.Properties.Any(p => p.Key == "pypi.version" && p.Value == component.Version));

            Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = repoName };
            Property fileNameProperty = new() { Name = Dataconstant.Cdx_Siemensfilename, Value = jfrogPackageNameWhlExten };
            Property jfrogRepoPathProperty = new() { Name = Dataconstant.Cdx_JfrogRepoPath, Value = jfrogRepoPath };
            Component componentVal = component;

            UpdatePythonKpiDataBasedOnRepo(artifactoryrepo.Value, appSettings);

            // Use common helper to set component properties and hashes
            CommonHelper.SetComponentPropertiesAndHashes(componentVal, artifactoryrepo, projectType, fileNameProperty, jfrogRepoPathProperty, hashes);

            return componentVal;
        }

        /// <summary>
        /// Updates Python Kpi Data Based On Repo
        /// </summary>
        /// <param name="repoValue"></param>
        /// <param name="appSettings"></param>
        private static void UpdatePythonKpiDataBasedOnRepo(string repoValue, CommonAppSettings appSettings)
        {
            if (repoValue == appSettings.Poetry.DevDepRepo)
            {
                BomCreator.bomKpiData.DevdependencyComponents++;
            }

            if (appSettings.Poetry.Artifactory.ThirdPartyRepos != null)
            {
                foreach (var thirdPartyRepo in appSettings.Poetry.Artifactory.ThirdPartyRepos)
                {
                    if (repoValue == thirdPartyRepo.Name)
                    {
                        BomCreator.bomKpiData.ThirdPartyRepoComponents++;
                        break;
                    }
                }
            }

            if (repoValue == appSettings.Poetry.ReleaseRepo)
            {
                BomCreator.bomKpiData.ReleaseRepoComponents++;
            }

            if (repoValue == Dataconstant.NotFoundInJFrog || repoValue == "")
            {
                BomCreator.bomKpiData.UnofficialComponents++;
            }
        }

        /// <summary>
        /// Gets Artifactory Repo Name
        /// </summary>
        /// <param name="aqlResultList"></param>
        /// <param name="component"></param>
        /// <param name="bomHelper"></param>
        /// <param name="jfrogPackageName"></param>
        /// <param name="jfrogRepoPath"></param>
        /// <returns>repo name</returns>
        private static string GetArtifactoryRepoName(List<AqlResult> aqlResultList,
                                                     Component component,
                                                     IBomHelper bomHelper,
                                                     out string jfrogPackageName,
                                                     out string jfrogRepoPath)
        {
            Logger.DebugFormat("GetArtifactoryRepoName(): Starting identify JFrog repository details retrieval for component [Name: {0}, Version: {1}].", component.Name, component.Version);
            jfrogPackageName = Dataconstant.PackageNameNotFoundInJfrog;
            jfrogRepoPath = Dataconstant.JfrogRepoPathNotFound;
            string jfrogPackageNameWhlExten = GetJfrogNameOfPypiComponent(
                component.Name, component.Version, aqlResultList);
            Logger.DebugFormat("GetArtifactoryRepoName(): Searching for component in JFrog repository with name: {0}.", jfrogPackageNameWhlExten);
            var aqlResults = aqlResultList.FindAll(x => x.Name.Equals(
                jfrogPackageNameWhlExten, StringComparison.OrdinalIgnoreCase));
            jfrogPackageName = jfrogPackageNameWhlExten;

            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);

            if (repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                string fullName = bomHelper.GetFullNameOfComponent(component);
                string fullNameVersion = GetJfrogNameOfPypiComponent(fullName, component.Version, aqlResultList);
                Logger.DebugFormat("GetArtifactoryRepoName(): Searching for component in JFrog repository with name: {0}.", fullNameVersion);
                if (!fullNameVersion.Equals(jfrogPackageNameWhlExten, StringComparison.OrdinalIgnoreCase))
                {
                    var aqllist = aqlResultList.FindAll(x => x.Name.Contains(fullNameVersion, StringComparison.OrdinalIgnoreCase)
                    && (x.Name.EndsWith(ApiConstant.PythonExtension) || x.Name.EndsWith(FileConstant.TargzFileExtension)));
                    jfrogPackageName = fullNameVersion;
                    repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqllist);
                }
            }

            // Forming Jfrog repo Path
            if (!repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                var aqlResult = aqlResults.FirstOrDefault(x => x.Repo.Equals(repoName));
                jfrogRepoPath = GetJfrogRepoPath(aqlResult);
                Logger.DebugFormat("GetJfrogArtifactoryRepoDetials(): JFrog repository path: {0}.", jfrogRepoPath);
            }

            if (string.IsNullOrEmpty(jfrogPackageName))
            {
                jfrogPackageName = Dataconstant.PackageNameNotFoundInJfrog;
            }

            return repoName;
        }

        /// <summary>
        /// Gets Jfrog Repo Path
        /// </summary>
        /// <param name="aqlResult"></param>
        /// <returns>repo path</returns>
        private static string GetJfrogRepoPath(AqlResult aqlResult)
        {
            if (string.IsNullOrEmpty(aqlResult.Path) || aqlResult.Path.Equals("."))
            {
                return $"{aqlResult.Repo}/{aqlResult.Name}";
            }

            return $"{aqlResult.Repo}/{aqlResult.Path}/{aqlResult.Name}";
        }

        /// <summary>
        /// Adds Component Properties
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="component"></param>
        private static void AddComponentProperties(PythonPackage prop, Component component)
        {
            var devDependency = new Property
            {
                Name = Dataconstant.Cdx_IsDevelopment,
                Value = prop.Isdevdependent ? "true" : "false"
            };

            component.Properties ??= new List<Property>();

            if (prop.SpdxComponentDetails.SpdxComponent)
            {
                AddSpdxProperties(prop, component);
            }
            else
            {
                AddIdentifierTypeProperty(prop, component, devDependency);
            }
        }

        /// <summary>
        /// Adds Spdx Properties
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="component"></param>
        private static void AddSpdxProperties(PythonPackage prop, Component component)
        {
            string fileName = Path.GetFileName(prop.SpdxComponentDetails.SpdxFilePath);
            SpdxSbomHelper.AddSpdxComponentProperties(fileName, component);
            SpdxSbomHelper.AddDevelopmentPropertyForSpdx(prop.SpdxComponentDetails.DevComponent, component);
        }
        private static void IdentifiedPythonPackages(string filepath, List<PythonPackage> packages)
        {

            if (packages == null || packages.Count == 0)
            {
                // Log a message indicating no packages were found
                Logger.DebugFormat("No Python packages were found in the file: {0}", filepath);
                return;
            }

            // Build the table
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine(LogHandlingHelper.LogSeparator);
            logBuilder.AppendLine($" PYTHON PACKAGES FOUND IN FILE: {filepath}");
            logBuilder.AppendLine(LogHandlingHelper.LogSeparator);
            logBuilder.AppendLine($"| {"Name",-40} | {"Version",-20} | {"PURL",-60} | {"DevDependent",-15} |");
            logBuilder.AppendLine(LogHandlingHelper.LogHeaderSeparator);

            foreach (var package in packages)
            {
                string devDependent = package.Isdevdependent ? "true" : "false";
                logBuilder.AppendLine($"| {package.Name,-40} | {package.Version,-20} | {package.PurlID,-60} | {devDependent,-15} |");
            }

            logBuilder.AppendLine(LogHandlingHelper.LogSeparator);

            // Log the table
            Logger.Debug(logBuilder.ToString());
        }

        /// <summary>
        /// Adds Identifier Type Property
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="component"></param>
        /// <param name="devDependency"></param>
        private static void AddIdentifierTypeProperty(PythonPackage prop, Component component, Property devDependency)
        {
            component.Properties ??= new List<Property>();

            string identifierTypeValue = prop.FoundType == Dataconstant.Discovered ? Dataconstant.Discovered : Dataconstant.ManullayAdded;
            var properties = component.Properties;
            CommonHelper.RemoveDuplicateAndAddProperty(ref properties,
                devDependency.Name,
                devDependency.Value);
            CommonHelper.RemoveDuplicateAndAddProperty(ref properties,
                Dataconstant.Cdx_IdentifierType,
                identifierTypeValue);
            component.Properties = properties;
        }

        /// <summary>
        /// Sets Spdx ComponentDetails
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="package"></param>
        /// <param name="componentInfo"></param>
        private static void SetSpdxComponentDetails(string filePath, PythonPackage package, Component componentInfo)
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
