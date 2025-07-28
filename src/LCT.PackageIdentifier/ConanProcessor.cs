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
using LCT.Common.Model;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;
using File = System.IO.File;

namespace LCT.PackageIdentifier
{

    /// <summary>
    /// Parses the Conan Packages
    /// </summary>
    public class ConanProcessor(ICycloneDXBomParser cycloneDXBomParser,ISpdxBomParser spdxBomParser) : CycloneDXBomParser, IParser
    {
        #region fields
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        private static readonly char[] SplitChars = { '/', '@' };
        private static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
        private List<Component> listOfInternalComponents = new List<Component>();
        private readonly IEnvironmentHelper _environmentHelper = new EnvironmentHelper();
        #endregion
        #region constructor
        #endregion

        #region public methods
        public Bom ParsePackageFile(CommonAppSettings appSettings,ref Bom unSupportedBomList)
        {
            Logger.Debug($"ParsePackageFile():Started Parseing input File for conan components.");
            List<Component> componentsForBOM;
            Bom bom = new Bom();
            int totalUnsupportedComponentsIdentified = 0;
            ParsingInputFileForBOM(appSettings, ref bom);
            componentsForBOM = bom.Components;

            componentsForBOM = GetExcludedComponentsList(componentsForBOM);
            componentsForBOM = componentsForBOM.Distinct(new ComponentEqualityComparer()).ToList();

            var componentsWithMultipleVersions = componentsForBOM.GroupBy(s => s.Name)
                              .Where(g => g.Count() > 1).SelectMany(g => g).ToList();

            if (componentsWithMultipleVersions.Count != 0)
            {
                CreateFileForMultipleVersions(componentsWithMultipleVersions, appSettings);
            }

            bom.Components = componentsForBOM;
            bom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(bom.Components, bom.Dependencies);
            totalUnsupportedComponentsIdentified = ListUnsupportedComponentsForBom.Components.Count;
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += ListUnsupportedComponentsForBom.Components.Count;
            ListUnsupportedComponentsForBom.Components = ListUnsupportedComponentsForBom.Components.Distinct(new ComponentEqualityComparer()).ToList();
            BomCreator.bomKpiData.DuplicateComponents += totalUnsupportedComponentsIdentified - ListUnsupportedComponentsForBom.Components.Count;
            ListUnsupportedComponentsForBom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(ListUnsupportedComponentsForBom.Components, ListUnsupportedComponentsForBom.Dependencies);
            unSupportedBomList.Components = ListUnsupportedComponentsForBom.Components;
            unSupportedBomList.Dependencies = ListUnsupportedComponentsForBom.Dependencies;            
            Logger.Debug($"ParsePackageFile():Completed Parseing input File for conan components.\n");
            return bom;
        }

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData, CommonAppSettings appSettings,
            IJFrogService jFrogService, IBomHelper bomhelper)
        {
            Logger.Debug("IdentificationOfInternalComponents(): Starting identification of internal components.");
            // get the  component list from Jfrog for given repository
            List<AqlResult> aqlResultList =
                await bomhelper.GetListOfComponentsFromRepo(appSettings.Conan.Artifactory.InternalRepos, jFrogService);

            var inputIterationList = componentData.comparisonBOMData;
            
            // Use the common helper method
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(
                inputIterationList, 
                component => IsInternalConanComponent(aqlResultList, component));

            // update the comparison bom data
            componentData.comparisonBOMData = processedComponents;
            componentData.internalComponents = internalComponents;
            listOfInternalComponents = internalComponents;
            Logger.Debug($"IdentificationOfInternalComponents(): identified internal components:{internalComponents.Count}.");
            Logger.Debug($"IdentificationOfInternalComponents(): Completed identification of internal components\n");
            return componentData;
        }


        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            Logger.Debug("GetJfrogRepoDetailsOfAComponent():Starting to retrieve JFrog repository details for components.\n");
            // get the  component list from Jfrog for given repo + internal repo
            string[] repoList = CommonHelper.GetRepoList(appSettings);
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(repoList, jFrogService);
            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                Component updatedComponent = UpdateComponentDetails(component, aqlResultList, appSettings, projectType);
                modifiedBOM.Add(updatedComponent);
            }
            LogHandlingHelper.IdentifierComponentsData(componentsForBOM, listOfInternalComponents);
            Logger.Debug("GetJfrogRepoDetailsOfAComponent():Completed retrieving JFrog repository details for components.\n");
            return modifiedBOM;
        }

        public static bool IsDevDependency(ConanPackage component, List<string> buildNodeIds, ref int noOfDevDependent)
        {
            var isDev = false;
            if (buildNodeIds != null && buildNodeIds.Contains(component.Id))
            {
                isDev = true;
                noOfDevDependent++;
            }

            return isDev;
        }

        #endregion

        #region private methods
        private static Component UpdateComponentDetails(Component component, List<AqlResult> aqlResultList, CommonAppSettings appSettings, Property projectType)
        {
            string repoName = GetArtifactoryRepoName(aqlResultList, component, out string jfrogRepoPath);
            string jfrogpackageName = $"{component.Name}/{component.Version}";
            Logger.Debug($"Repo Name for the package {jfrogpackageName} is {repoName}");

            var hashes = aqlResultList.FirstOrDefault(x => x.Path.Contains(jfrogpackageName, StringComparison.OrdinalIgnoreCase));
            Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = repoName };
            Property jfrogRepoPathProperty = new() { Name = Dataconstant.Cdx_JfrogRepoPath, Value = jfrogRepoPath };

            UpdateBomKpiData(appSettings, artifactoryrepo.Value);

            if (component.Properties?.Count == null || component.Properties?.Count <= 0)
            {
                component.Properties = new List<Property>();
            }

            component.Properties.Add(artifactoryrepo);
            component.Properties.Add(projectType);
            component.Properties.Add(jfrogRepoPathProperty);
            component.Description = null;

            if (hashes != null)
            {
                component.Hashes = GetComponentHashes(hashes);
            }

            return component;
        }

        private static void UpdateBomKpiData(CommonAppSettings appSettings, string repoValue)
        {
            if (repoValue == appSettings.Conan.DevDepRepo)
            {
                BomCreator.bomKpiData.DevdependencyComponents++;
            }
            else if (appSettings.Conan.Artifactory.ThirdPartyRepos != null && appSettings.Conan.Artifactory.ThirdPartyRepos.Any(repo => repo.Name == repoValue))
            {
                BomCreator.bomKpiData.ThirdPartyRepoComponents++;
            }
            else if (repoValue == appSettings.Conan.ReleaseRepo)
            {
                BomCreator.bomKpiData.ReleaseRepoComponents++;
            }
            else if (repoValue == Dataconstant.NotFoundInJFrog || string.IsNullOrEmpty(repoValue))
            {
                BomCreator.bomKpiData.UnofficialComponents++;
            }
        }

        private static List<Hash> GetComponentHashes(AqlResult hashes)
        {
            return new List<Hash>
    {
        new() { Alg = Hash.HashAlgorithm.MD5, Content = hashes.MD5 },
        new() { Alg = Hash.HashAlgorithm.SHA_1, Content = hashes.SHA1 },
        new() { Alg = Hash.HashAlgorithm.SHA_256, Content = hashes.SHA256 }
    };
        }

        public static void CreateFileForMultipleVersions(List<Component> componentsWithMultipleVersions, CommonAppSettings appSettings)
        {
            MultipleVersions multipleVersions = new MultipleVersions();
            FileOperations fileOperations = new FileOperations();
            string defaultProjectName = CommonIdentiferHelper.GetDefaultProjectName(appSettings);
            string bomFullPath = $"{appSettings.Directory.OutputFolder}\\{defaultProjectName}_Bom.cdx.json";

            string filePath = $"{appSettings.Directory.OutputFolder}\\{defaultProjectName}_{FileConstant.multipleversionsFileName}";
            if (!File.Exists(filePath))
            {
                multipleVersions.Conan = new List<MultipleVersionValues>();
                foreach (var conanPackage in componentsWithMultipleVersions)
                {
                    conanPackage.Description = !string.IsNullOrEmpty(bomFullPath) ? bomFullPath : conanPackage.Description;
                    MultipleVersionValues jsonComponents = new MultipleVersionValues();
                    jsonComponents.ComponentName = conanPackage.Name;
                    jsonComponents.ComponentVersion = conanPackage.Version;
                    jsonComponents.PackageFoundIn = conanPackage.Description;
                    multipleVersions.Conan.Add(jsonComponents);
                }
                fileOperations.WriteContentToMultipleVersionsFile(multipleVersions, appSettings.Directory.OutputFolder, FileConstant.multipleversionsFileName, defaultProjectName);
                Logger.Warn($"\nTotal Multiple versions detected {multipleVersions.Conan.Count} and details can be found at {filePath}\n");
            }
            else
            {
                string json = File.ReadAllText(filePath);
                MultipleVersions myDeserializedClass = JsonConvert.DeserializeObject<MultipleVersions>(json);
                List<MultipleVersionValues> conanComponents = new List<MultipleVersionValues>();
                foreach (var conanPackage in componentsWithMultipleVersions)
                {

                    MultipleVersionValues jsonComponents = new MultipleVersionValues();
                    jsonComponents.ComponentName = conanPackage.Name;
                    jsonComponents.ComponentVersion = conanPackage.Version;
                    jsonComponents.PackageFoundIn = conanPackage.Description;

                    conanComponents.Add(jsonComponents);
                }
                myDeserializedClass.Conan = conanComponents;

                fileOperations.WriteContentToMultipleVersionsFile(myDeserializedClass, appSettings.Directory.OutputFolder, FileConstant.multipleversionsFileName, defaultProjectName);
                Logger.Warn($"\nTotal Multiple versions detected {conanComponents.Count} and details can be found at {filePath}\n");
            }
        }
        private void ParsingInputFileForBOM(CommonAppSettings appSettings, ref Bom bom)
        {
            Logger.Debug("ParsingInputFileForBOM():Starting parsing of input files for BOM.");
            List<string> configFiles;
            List<Dependency> dependencies = new List<Dependency>();
            List<Component> componentsForBOM = new List<Component>();
            configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Conan,_environmentHelper);
            List<string> listOfTemplateBomfilePaths = new List<string>();
            foreach (string filepath in configFiles)
            {
                if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    Logger.Debug($"ParsingInputFileForBOM():Template BOM file detected: {filepath}");
                    listOfTemplateBomfilePaths.Add(filepath);
                }
                if (filepath.ToLower().EndsWith("conan.lock"))
                {
                    Logger.Debug($"ParsingInputFileForBOM():conan.lock file detected: {filepath}");
                    var components = ParsePackageLockJson(filepath, ref dependencies);
                    AddingIdentifierType(components, "PackageFile");
                    componentsForBOM.AddRange(components);
                    LogHandlingHelper.IdentifierInputFileComponents(filepath, components);
                }
                else if (filepath.EndsWith(FileConstant.SPDXFileExtension))
                {
                    Logger.Debug($"ParsingInputFileForBOM():Spdx file detected: {filepath}");
                    Bom listUnsupportedComponents = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
                    bom = _spdxBomParser.ParseSPDXBom(filepath);
                    LogHandlingHelper.IdentifierInputFileComponents(filepath, bom.Components);
                    SpdxSbomHelper.CheckValidComponentsFromSpdxfile(bom, appSettings.ProjectType,ref listUnsupportedComponents);
                    SpdxSbomHelper.AddSpdxSBomFileNameProperty(ref bom, filepath);
                    componentsForBOM.AddRange(bom.Components);
                    dependencies.AddRange(bom.Dependencies);
                    SpdxSbomHelper.AddSpdxPropertysForUnsupportedComponents(listUnsupportedComponents.Components,filepath);
                    ListUnsupportedComponentsForBom.Components.AddRange(listUnsupportedComponents.Components);
                    ListUnsupportedComponentsForBom.Dependencies.AddRange(listUnsupportedComponents.Dependencies);                    
                }
                else if (filepath.EndsWith(FileConstant.CycloneDXFileExtension)
                    && !filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    Logger.Debug($"ParsingInputFileForBOM():CycloneDX file detected: {filepath}");
                    bom = _cycloneDXBomParser.ParseCycloneDXBom(filepath);
                    CheckValidComponentsForProjectType(bom.Components, appSettings.ProjectType);
                    GetDetailsforManuallyAddedComp(bom.Components);
                    componentsForBOM.AddRange(bom.Components);
                    LogHandlingHelper.IdentifierInputFileComponents(filepath, bom.Components);
                }
            }

            int initialCount = componentsForBOM.Count;
            GetDistinctComponentList(ref componentsForBOM);
            BomCreator.bomKpiData.DuplicateComponents = initialCount - componentsForBOM.Count;
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = componentsForBOM.Count;
            bom.Components = componentsForBOM;

            if (bom.Dependencies != null)
            {
                bom.Dependencies.AddRange(dependencies);
            }
            else
            {
                bom.Dependencies = dependencies;
            }
            string templateFilePath = SbomTemplate.GetFilePathForTemplate(listOfTemplateBomfilePaths);
            SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, bom.Components, appSettings.ProjectType);

            bom = RemoveExcludedComponents(appSettings, bom);
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
            Logger.Debug("ParsingInputFileForBOM():Completed parsing of input files for BOM.");
        }

        private static List<Component> ParsePackageLockJson(string filepath, ref List<Dependency> dependencies)
        {
            List<Component> lstComponentForBOM = new List<Component>();
            int noOfDevDependent = 0;

            try
            {
                string jsonContent = File.ReadAllText(filepath);
                var jsonDeserialized = JObject.Parse(jsonContent);
                var nodes = jsonDeserialized["graph_lock"]["nodes"];

                List<ConanPackage> nodePackages = new List<ConanPackage>();
                foreach (var node in nodes)
                {
                    string nodeId = ((JProperty)node).Name;
                    var conanPackage = JsonConvert.DeserializeObject<ConanPackage>(((JProperty)node).Value.ToString());
                    conanPackage.Id = nodeId;
                    nodePackages.Add(conanPackage);
                }

                GetPackagesForBom(ref lstComponentForBOM, ref noOfDevDependent, nodePackages);

                GetDependecyDetails(lstComponentForBOM, nodePackages, dependencies);

                BomCreator.bomKpiData.DevDependentComponents += noOfDevDependent;
            }
            catch (JsonReaderException ex)
            {
                Environment.ExitCode = -1;
                LogHandlingHelper.ExceptionErrorHandling("Error occurred while parsing the package lock JSON file.", "ParsePackageLockJson()", ex, $"File Path: {filepath}");
            }
            catch (IOException ex)
            {
                Environment.ExitCode = -1;
                LogHandlingHelper.ExceptionErrorHandling("I/O error occurred while accessing the package lock JSON file.", "ParsePackageLockJson()", ex, $"File Path: {filepath}");
            }
            catch (SecurityException ex)
            {
                Environment.ExitCode = -1;
                LogHandlingHelper.ExceptionErrorHandling("Security error occurred while accessing the package lock JSON file.", "ParsePackageLockJson()", ex, $"File Path: {filepath}");
            }

            return lstComponentForBOM;
        }

        private static void GetDependecyDetails(List<Component> componentsForBOM, List<ConanPackage> nodePackages, List<Dependency> dependencies)
        {
            foreach (Component component in componentsForBOM)
            {
                var node = nodePackages.Find(x => x.Reference.Contains($"{component.Name}/{component.Version}"));
                var dependencyNodes = new List<ConanPackage>();
                if (node.Dependencies != null && node.Dependencies.Count > 0)
                {
                    dependencyNodes.AddRange(nodePackages.Where(x => node.Dependencies.Contains(x.Id)).ToList());
                }
                if (node.DevDependencies != null && node.DevDependencies.Count > 0)
                {
                    dependencyNodes.AddRange(nodePackages.Where(x => node.DevDependencies.Contains(x.Id)).ToList());
                }
                var dependency = new Dependency();
                var subDependencies = componentsForBOM.Where(x => dependencyNodes.Exists(y => y.Reference.Contains($"{x.Name}/{x.Version}")))
                                        .Select(x => new Dependency { Ref = x.Purl }).ToList();

                dependency.Ref = component.Purl;
                dependency.Dependencies = subDependencies;

                if (subDependencies.Count > 0)
                {
                    dependencies.Add(dependency);
                }
            }
        }

        private static void GetPackagesForBom(ref List<Component> lstComponentForBOM, ref int noOfDevDependent, List<ConanPackage> nodePackages)
        {
            ValidateRootNode(nodePackages);

            List<string> directDependencies = GetDirectDependencies(nodePackages);

            // Ignoring the root node as it is the package information node and we are anyways considering all
            // nodes in the lock file.
            foreach (var component in nodePackages.Skip(1))
            {
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += 1;
                Property isdev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };

                if (string.IsNullOrEmpty(component.Reference))
                {
                    BomCreator.bomKpiData.ComponentsinPackageLockJsonFile--;
                    continue;
                }

                Component components = new Component();

                // dev components are not ignored and added as a part of SBOM   
                var buildNodeIds = GetBuildNodeIds(nodePackages);
                if (IsDevDependency(component, buildNodeIds, ref noOfDevDependent))
                {
                    isdev.Value = "true";
                }

                string packageName = Convert.ToString(component.Reference);

                if (packageName.Contains('/'))
                {
                    components.Name = packageName.Split(SplitChars)[0];
                    components.Version = packageName.Split(SplitChars)[1];
                }
                else
                {
                    components.Name = packageName;
                }

                Property siemensFileName = new Property()
                {
                    Name = Dataconstant.Cdx_Siemensfilename,
                    Value = component.Reference
                };
                var isDirect = directDependencies.Contains(component.Id) ? "true" : "false";
                Property siemensDirect = new Property()
                {
                    Name = Dataconstant.Cdx_SiemensDirect,
                    Value = isDirect
                };

                components.Type = Component.Classification.Library;
                components.Purl = $"{ApiConstant.ConanExternalID}{components.Name}@{components.Version}";
                components.BomRef = $"{ApiConstant.ConanExternalID}{components.Name}@{components.Version}";
                components.Properties = new List<Property>();
                components.Properties.Add(isdev);
                components.Properties.Add(siemensDirect);
                components.Properties.Add(siemensFileName);
                lstComponentForBOM.Add(components);
            }
        }

        private static List<string> GetBuildNodeIds(List<ConanPackage> nodePackages)
        {
            return nodePackages
                    .Where(y => y.DevDependencies != null)
                    .SelectMany(y => y.DevDependencies)
                    .ToList();
        }
        private static void ValidateRootNode(List<ConanPackage> nodePackages)
        {
            var rootNode = nodePackages.FirstOrDefault();
            if (rootNode != null && (rootNode.Dependencies.Count == 0 || rootNode.Dependencies == null))
            {
                throw new ArgumentNullException(nameof(nodePackages), "Dependency(requires) node name details not present in the root node.");
            }
        }
        private static List<string> GetDirectDependencies(List<ConanPackage> nodePackages)
        {
            List<string> directDependencies = new List<string>();
            ConanPackage package = nodePackages.FirstOrDefault(x => x.Id == "0");

            if (package != null)
            {
                if (package.Dependencies != null)
                {
                    directDependencies.AddRange(package.Dependencies);
                }

                if (package.DevDependencies != null)
                {
                    directDependencies.AddRange(package.DevDependencies);
                }
            }

            return directDependencies;
        }
        private static bool IsInternalConanComponent(List<AqlResult> aqlResultList, Component component)
        {
            string jfrogcomponentPath = $"{component.Name}/{component.Version}";
            if (aqlResultList.Exists(
                x => x.Path.Contains(jfrogcomponentPath, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.Debug($"IsInternalConanComponent(): Component [Name: {component.Name}, Version: {component.Version}] is internal,Found in JFrog repository with full name: {jfrogcomponentPath}.");
                return true;
            }

            return false;
        }

        public static string GetArtifactoryRepoName(List<AqlResult> aqlResultList, Component component, out string jfrogRepoPath)
        {
            Logger.Debug($"GetArtifactoryRepoName(): Starting identify JFrog repository details retrieval for component [Name: {component.Name}, Version: {component.Version}].");
            string jfrogcomponentPath = $"{component.Name}/{component.Version}";
            Logger.Debug($"GetArtifactoryRepoName(): Searching for component in JFrog repository with name: {jfrogcomponentPath}.");
            jfrogRepoPath = Dataconstant.JfrogRepoPathNotFound;
            var conanPackagePath = aqlResultList.FirstOrDefault(x => x.Path.Contains(jfrogcomponentPath) && x.Name.Contains("package.tgz"));
            if (conanPackagePath != null)
            {
                jfrogRepoPath = $"{conanPackagePath.Repo}/{conanPackagePath.Path}/{conanPackagePath.Name};";
            }
            var aqllist = aqlResultList.FindAll(x => x.Path.Contains(
                jfrogcomponentPath, StringComparison.OrdinalIgnoreCase));

            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqllist);
            return repoName;
        }

        private static List<Component> GetExcludedComponentsList(List<Component> componentsForBOM)
        {
            Logger.Debug($"GetExcludedComponentsList():Start process of remove invalid components from full list of components");
            List<Component> components = new List<Component>();
            foreach (Component componentsInfo in componentsForBOM)
            {
                if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.Purl) && componentsInfo.Purl.Contains(Dataconstant.PurlCheck()["CONAN"]))
                {
                    components.Add(componentsInfo);
                }
                else
                {
                    BomCreator.bomKpiData.ComponentsExcluded++;
                    Logger.Debug($"GetExcludedComponentsList():InvalidComponent For CONAN : Component Details : {componentsInfo.Name} @ {componentsInfo.Version} @ {componentsInfo.Purl}");
                }
            }
            Logger.Debug($"GetExcludedComponentsList():Completed the process of remove invalid components from full list of components");
            return components;
        }

        private static void AddingIdentifierType(List<Component> components, string identifiedBy)
        {
            foreach (var component in components)
            {
                component.Properties ??= new List<Property>();

                Property isDev;
                Property identifierType;
                if (identifiedBy == "PackageFile")
                {
                    identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.Discovered };
                    component.Properties.Add(identifierType);
                }
                else
                {
                    isDev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };
                    identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.ManullayAdded };
                    component.Properties.Add(isDev);
                    component.Properties.Add(identifierType);
                }
            }
        }

        private static void GetDistinctComponentList(ref List<Component> listofComponents)
        {
            int initialCount = listofComponents.Count;
            listofComponents = listofComponents.GroupBy(x => new { x.Name, x.Version, x.Purl }).Select(y => y.First()).ToList();

            if (listofComponents.Count != initialCount)
                BomCreator.bomKpiData.DuplicateComponents = initialCount - listofComponents.Count;
        }

        private static Bom RemoveExcludedComponents(CommonAppSettings appSettings, Bom cycloneDXBOM)
        {
            return CommonHelper.RemoveExcludedComponentsFromBom(appSettings, cycloneDXBOM, 
                noOfExcludedComponents => BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents);
        }

        private static void GetDetailsforManuallyAddedComp(List<Component> componentsForBOM)
        {
            foreach (var component in componentsForBOM)
            {
                // check existence of property and add new
                component.Properties = new List<Property>();
                Property isDev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };
                Property identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.ManullayAdded };
                component.Properties.Add(isDev);
                component.Properties.Add(identifierType);
            }
        }             

        #endregion
    }
}