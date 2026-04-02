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
    /// Parses the Conan Packages (dep.json format)
    /// </summary>
    public class ConanProcessor(ICycloneDXBomParser cycloneDXBomParser, ISpdxBomParser spdxBomParser) : CycloneDXBomParser, IParser
    {
        #region Fields
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        private static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
        #endregion

        private readonly IEnvironmentHelper environmentHelper = new EnvironmentHelper();
        private List<Component> listOfInternalComponents = new List<Component>();
        #region Properties
        #endregion

        #region Constructors
        // Primary constructor parameters are declared on the class.
        #endregion

        #region Methods
        /// <summary>
        /// Parses Conan dep.json input files and produces a CycloneDX BOM for the project.
        /// </summary>
        /// <param name="appSettings">Application settings containing input folder and project type.</param>
        /// <param name="unSupportedBomList">Reference BOM that will be filled with unsupported components found during parsing.</param>
        /// <returns>Constructed CycloneDX BOM representing discovered components and dependencies.</returns>
        public Bom ParsePackageFile(CommonAppSettings appSettings, ref Bom unSupportedBomList)
        {
            Logger.Debug("ParsePackageFile():Started Parseing input File for conan components.");
            List<Component> componentsForBOM;
            Bom bom = new Bom();
            int totalUnsupportedComponentsIdentified = 0;
            ParsingInputFileForBOM(appSettings, ref bom);
            componentsForBOM = bom.Components;

            componentsForBOM = BomHelper.GetExcludedComponentsList(componentsForBOM, Dataconstant.PurlCheck()["CONAN"], appSettings?.ProjectType);
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
            Logger.Debug("ParsePackageFile():Completed Parseing input File for conan components.\n");
            return bom;
        }

        /// <summary>
        /// Asynchronously identifies internal Conan components by comparing BOM components to JFrog AQL results.
        /// </summary>
        /// <param name="componentData">Component identification input containing comparison BOM data.</param>
        /// <param name="appSettings">Application settings with repository configuration.</param>
        /// <param name="jFrogService">JFrog service used to query AQL results.</param>
        /// <param name="bomhelper">BOM helper providing repository query helpers.</param>
        /// <returns>Asynchronously returns the updated component identification data with internal components separated.</returns>
        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData, CommonAppSettings appSettings,
            IJFrogService jFrogService, IBomHelper bomhelper)
        {
            Logger.Debug("IdentificationOfInternalComponents(): Starting identification of internal components.");
            List<AqlResult> aqlResultList =
                await bomhelper.GetListOfComponentsFromRepo(appSettings.Conan.Artifactory.InternalRepos, jFrogService);
            var inputIterationList = componentData.comparisonBOMData;
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(
                inputIterationList,
                component => IsInternalConanComponent(aqlResultList, component));
            componentData.comparisonBOMData = processedComponents;
            componentData.internalComponents = internalComponents;
            listOfInternalComponents = internalComponents;
            Logger.DebugFormat("IdentificationOfInternalComponents(): identified internal components:{0}.", internalComponents.Count);
            Logger.Debug("IdentificationOfInternalComponents(): Completed identification of internal components\n");
            return componentData;
        }


        /// <summary>
        /// Asynchronously enriches the given components with JFrog repository details and returns the modified list.
        /// </summary>
        /// <param name="componentsForBOM">List of components to enrich with JFrog details.</param>
        /// <param name="appSettings">Application settings providing repo lists and configuration.</param>
        /// <param name="jFrogService">JFrog service used to query AQL results.</param>
        /// <param name="bomhelper">BOM helper utilities for repository queries.</param>
        /// <returns>Asynchronously returns a new list of components with repository properties and hashes set.</returns>
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

        /// <summary>
        /// Updates a single component's properties (repo, path, project type) and populates hashes if available.
        /// </summary>
        /// <param name="component">Component to update.</param>
        /// <param name="aqlResultList">AQL results used to find repository entries and hashes.</param>
        /// <param name="appSettings">Application settings for repo configuration.</param>
        /// <param name="projectType">Property representing the project type to attach to the component.</param>
        /// <returns>Updated component instance.</returns>
        private static Component UpdateComponentDetails(Component component, List<AqlResult> aqlResultList, CommonAppSettings appSettings, Property projectType)
        {
            string repoName = GetArtifactoryRepoName(aqlResultList, component, out string jfrogRepoPath);
            string jfrogpackageName = $"{component.Name}/{component.Version}";
            Logger.DebugFormat("Repo Name for the package {0} is {1}", jfrogpackageName, repoName);

            var hashes = aqlResultList.FirstOrDefault(x => x.Path.Contains(jfrogpackageName, StringComparison.OrdinalIgnoreCase));
            Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = repoName };
            Property jfrogRepoPathProperty = new() { Name = Dataconstant.Cdx_JfrogRepoPath, Value = jfrogRepoPath };

            UpdateBomKpiData(appSettings, artifactoryrepo.Value);

            component.Properties ??= new List<Property>();
            var properties = component.Properties;
            CommonHelper.RemoveDuplicateAndAddProperty(ref properties, artifactoryrepo.Name, artifactoryrepo.Value);
            CommonHelper.RemoveDuplicateAndAddProperty(ref properties, projectType?.Name, projectType?.Value);
            CommonHelper.RemoveDuplicateAndAddProperty(ref properties, jfrogRepoPathProperty.Name, jfrogRepoPathProperty.Value);
            component.Properties = properties;
            component.Description = null;

            if (hashes != null)
            {
                component.Hashes = GetComponentHashes(hashes);
            }

            return component;
        }

        /// <summary>
        /// Updates KPI counters depending on repository type (devdep, third-party, release or not found).
        /// </summary>
        /// <param name="appSettings">Application settings containing repository names.</param>
        /// <param name="repoValue">Repository name that the component was found in.</param>
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

        /// <summary>
        /// Converts AQL hash result into a CycloneDX Hash list.
        /// </summary>
        /// <param name="hashes">AQL result containing MD5, SHA1 and SHA256 values.</param>
        /// <returns>List of CycloneDX Hash objects.</returns>
        private static List<Hash> GetComponentHashes(AqlResult hashes)
        {
            return new List<Hash>
    {
        new() { Alg = Hash.HashAlgorithm.MD5, Content = hashes.MD5 },
        new() { Alg = Hash.HashAlgorithm.SHA_1, Content = hashes.SHA1 },
        new() { Alg = Hash.HashAlgorithm.SHA_256, Content = hashes.SHA256 }
    };
        }

        /// <summary>
        /// Writes a file enumerating components that have multiple versions detected in the project.
        /// </summary>
        /// <param name="componentsWithMultipleVersions">List of components that have multiple versions.</param>
        /// <param name="appSettings">Application settings used to determine output paths.</param>
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
                Logger.WarnFormat("\nTotal Multiple versions detected {0} and details can be found at {1}\n",
                    multipleVersions.Conan.Count, filePath);
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
                Logger.WarnFormat("\nTotal Multiple versions detected {0} and details can be found at {1}\n",
                conanComponents.Count, filePath);
            }
        }

        /// <summary>
        /// Scans input folder, parses supported files and populates the BOM with components and dependencies.
        /// </summary>
        /// <param name="appSettings">Application settings containing input folder and type-specific config.</param>
        /// <param name="bom">Reference BOM to populate.</param>
        private void ParsingInputFileForBOM(CommonAppSettings appSettings, ref Bom bom)
        {
            Logger.Debug("ParsingInputFileForBOM():Starting parsing of input files for BOM.");
            List<string> configFiles;
            List<Dependency> dependencies = new List<Dependency>();
            List<Component> componentsForBOM = new List<Component>();
            configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Conan, environmentHelper);
            List<string> listOfTemplateBomfilePaths = new List<string>();

            foreach (string filepath in configFiles)
            {
                if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listOfTemplateBomfilePaths.Add(filepath);
                    Logger.DebugFormat("ParsingInputFileForBOM():Template BOM file detected: {0}", filepath);
                }
                if (filepath.ToLower().EndsWith(FileConstant.ConanFileExtension))
                {
                    Logger.DebugFormat("ParsingInputFileForBOM():conan.lock file detected: {0}", filepath);
                    var components = ParseDepJson(filepath, ref dependencies);
                    AddingIdentifierType(components, "PackageFile");
                    componentsForBOM.AddRange(components);
                    LogHandlingHelper.IdentifierInputFileComponents(filepath, components);
                }
                else if (filepath.EndsWith(FileConstant.SPDXFileExtension))
                {
                    Logger.DebugFormat("ParsingInputFileForBOM():Spdx file detected: {0}", filepath);
                    BomHelper.NamingConventionOfSPDXFile(filepath, appSettings);
                    Bom listUnsupportedComponents = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
                    bom = _spdxBomParser.ParseSPDXBom(filepath);
                    SpdxSbomHelper.CheckValidComponentsFromSpdxfile(bom, appSettings.ProjectType, ref listUnsupportedComponents);
                    SpdxSbomHelper.AddSpdxSBomFileNameProperty(ref bom, filepath);
                    componentsForBOM.AddRange(bom.Components);
                    dependencies.AddRange(bom.Dependencies);
                    LogHandlingHelper.IdentifierInputFileComponents(filepath, bom.Components);
                    SpdxSbomHelper.AddSpdxPropertysForUnsupportedComponents(listUnsupportedComponents.Components, filepath);
                    ListUnsupportedComponentsForBom.Components.AddRange(listUnsupportedComponents.Components);
                    ListUnsupportedComponentsForBom.Dependencies.AddRange(listUnsupportedComponents.Dependencies);
                }
                else if (filepath.EndsWith(FileConstant.CycloneDXFileExtension)
                    && !filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    Logger.DebugFormat("ParsingInputFileForBOM():CycloneDX file detected: {0}", filepath);
                    bom = _cycloneDXBomParser.ParseCycloneDXBom(filepath);
                    CheckValidComponentsForProjectType(bom.Components, appSettings.ProjectType);
                    BomHelper.GetDetailsforManuallyAddedComp(bom.Components);
                    componentsForBOM.AddRange(bom.Components);
                    LogHandlingHelper.IdentifierInputFileComponents(filepath, bom.Components);
                }
            }

            int initialCount = componentsForBOM.Count;
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = componentsForBOM.Count;
            BomHelper.GetDistinctComponentList(ref componentsForBOM);
            BomCreator.bomKpiData.DuplicateComponents = initialCount - componentsForBOM.Count;

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

            bom = BomHelper.RemoveExcludedComponents(appSettings, bom);
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
            Logger.Debug("ParsingInputFileForBOM():Completed parsing of input files for BOM.");
        }

        /// <summary>
        /// Parses a Conan dep.json file and returns a list of CycloneDX components derived from it.
        /// </summary>
        /// <param name="filepath">Path to the dep.json file.</param>
        /// <param name="dependencies">Reference list that will be populated with dependency relationships.</param>
        /// <returns>List of components extracted from the dep.json file.</returns>
        private static List<Component> ParseDepJson(string filepath, ref List<Dependency> dependencies)
        {
            List<Component> StartingComponentForBOM = new List<Component>();
            int noOfDevDependent = 0;

            try
            {
                string jsonContent = File.ReadAllText(filepath);
                var depJson = JsonConvert.DeserializeObject<ConanDepJson>(jsonContent);

                if (depJson?.Graph?.Nodes == null)
                {
                    Logger.WarnFormat("No nodes found in dep.json file: {0}", filepath);
                    return StartingComponentForBOM;
                }

                var nodePackages = depJson.Graph.Nodes.ToList();

                GetPackagesForBom(ref StartingComponentForBOM, ref noOfDevDependent, nodePackages);
                GetDependencyDetails(StartingComponentForBOM, nodePackages, dependencies);

                BomCreator.bomKpiData.DevDependentComponents += noOfDevDependent;
            }
            catch (JsonReaderException ex)
            {
                Environment.ExitCode = -1;
                LogHandlingHelper.ExceptionErrorHandling("Error occurred while parsing the package lock JSON file.", "ParseDepJson()", ex, $"File Path: {filepath}");
                Logger.Error($"ParseDepJson(): Failed to parse JSON", ex);
            }
            catch (IOException ex)
            {
                Environment.ExitCode = -1;
                LogHandlingHelper.ExceptionErrorHandling("I/O error occurred while accessing the package lock JSON file.", "ParseDepJson()", ex, $"File Path: {filepath}");
                Logger.Error("ParseDepJson(): IO Error", ex);
            }
            catch (SecurityException ex)
            {
                Environment.ExitCode = -1;
                LogHandlingHelper.ExceptionErrorHandling("Security error occurred while accessing the package lock JSON file.", "ParseDepJson()", ex, $"File Path: {filepath}");
                Logger.Error("ParseDepJson(): Security Error", ex);
            }

            return StartingComponentForBOM;
        }

        /// <summary>
        /// Extracts components from node packages and updates the dev-dependency counter.
        /// </summary>
        /// <param name="StartingComponentForBOM">Reference list to add components to.</param>
        /// <param name="noOfDevDependent">Reference counter for development dependencies.</param>
        /// <param name="nodePackages">List of node id / ConanPackage pairs.</param>
        private static void GetPackagesForBom(ref List<Component> StartingComponentForBOM, ref int noOfDevDependent,
            List<KeyValuePair<string, ConanPackage>> nodePackages)
        {
            // Get root node to determine direct dependencies
            var rootNode = nodePackages.FirstOrDefault(n => n.Key == "0");

            foreach (var node in nodePackages)
            {
                var nodeId = node.Key;
                var package = node.Value;

                // Skip root consumer node (usually "0") - this is the project itself, not a dependency
                if (nodeId == "0")
                    continue;

                // Skip nodes without name or version (these are not real components)
                if (string.IsNullOrEmpty(package.Name) || string.IsNullOrEmpty(package.Version))
                {
                    continue;
                }

                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += 1;

                Component component = new Component();
                Property isdev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };

                // Determine if this is a development dependency using Conan logic
                if (IsDevDependency(package, ref noOfDevDependent))
                {
                    isdev.Value = "true";
                }

                component.Name = package.Name;
                component.Version = package.Version;

                Property siemensFileName = new Property()
                {
                    Name = Dataconstant.Cdx_Siemensfilename,
                    Value = $"{package.Name}/{package.Version}"
                };

                // Determine if this is a direct dependency by checking the "direct" property 
                // in the root node's dependency relationship
                bool isDirect = false;
                if (rootNode.Value?.Dependencies != null && rootNode.Value.Dependencies.TryGetValue(nodeId, out var depInfo))
                {
                    // Use the "direct" property from the dependency relationship in the root node
                    isDirect = depInfo.Direct == true;
                }

                Property siemensDirect = new Property()
                {
                    Name = Dataconstant.Cdx_SiemensDirect,
                    Value = isDirect ? "true" : "false"
                };

                component.Type = Component.Classification.Library;
                component.Purl = $"{ApiConstant.ConanExternalID}{component.Name}@{component.Version}";
                component.BomRef = $"{ApiConstant.ConanExternalID}{component.Name}@{component.Version}";
                component.Properties = new List<Property>();
                component.Properties.Add(isdev);
                component.Properties.Add(siemensDirect);
                component.Properties.Add(siemensFileName);
                StartingComponentForBOM.Add(component);
            }
        }

        /// <summary>
        /// Determines whether a Conan package represents a development dependency.
        /// </summary>
        /// <param name="package">Conan package to evaluate.</param>
        /// <param name="noOfDevDependent">Reference counter incremented when a dev dependency is found.</param>
        /// <returns>True if package is a development dependency; otherwise false.</returns>
        public static bool IsDevDependency(ConanPackage package, ref int noOfDevDependent)
        {
            // For Conan 2.0, dev dependencies are identified by context = "build"
            bool isDev = package.Context == "build";

            if (isDev)
            {
                noOfDevDependent++;
            }

            return isDev;
        }

        /// <summary>
        /// Builds CycloneDX dependency relationships from Conan node package information.
        /// </summary>
        /// <param name="componentsForBOM">List of components for which to build dependencies.</param>
        /// <param name="nodePackages">Node package map from the dep.json graph.</param>
        /// <param name="dependencies">List to populate with CycloneDX Dependency objects.</param>
        private static void GetDependencyDetails(List<Component> componentsForBOM,
            List<KeyValuePair<string, ConanPackage>> nodePackages, List<Dependency> dependencies)
        {
            foreach (Component component in componentsForBOM)
            {
                var node = nodePackages.FirstOrDefault(x => x.Value.Name == component.Name && x.Value.Version == component.Version);

                if (node.Value?.Dependencies == null || node.Value.Dependencies.Count == 0)
                    continue;

                var subDependencies = new List<Dependency>();

                foreach (var dep in node.Value.Dependencies)
                {
                    var dependentNode = nodePackages.FirstOrDefault(x => x.Key == dep.Key);
                    if (dependentNode.Value != null && !string.IsNullOrEmpty(dependentNode.Value.Name))
                    {
                        string depPurl = $"{ApiConstant.ConanExternalID}{dependentNode.Value.Name}@{dependentNode.Value.Version}";
                        subDependencies.Add(new Dependency { Ref = depPurl });
                    }
                }

                if (subDependencies.Count > 0)
                {
                    var dependency = new Dependency()
                    {
                        Ref = component.Purl,
                        Dependencies = subDependencies
                    };
                    dependencies.Add(dependency);
                }
            }
        }

        /// <summary>
        /// Adds identification-related properties to components depending on the identification source.
        /// </summary>
        /// <param name="components">Components to update.</param>
        /// <param name="identifiedBy">Identifier source name (e.g., "PackageFile" or manual).</param>
        private static void AddingIdentifierType(List<Component> components, string identifiedBy)
        {
            foreach (var component in components)
            {
                component.Properties ??= new List<Property>();

                if (identifiedBy == "PackageFile")
                {
                    var properties = component.Properties;
                    CommonHelper.RemoveDuplicateAndAddProperty(ref properties,
                        Dataconstant.Cdx_IdentifierType,
                        Dataconstant.Discovered);
                    component.Properties = properties;
                }
                else
                {
                    var properties = component.Properties;
                    CommonHelper.RemoveDuplicateAndAddProperty(ref properties,
                        Dataconstant.Cdx_IsDevelopment,
                        "false");
                    CommonHelper.RemoveDuplicateAndAddProperty(ref properties,
                        Dataconstant.Cdx_IdentifierType,
                        Dataconstant.ManullayAdded);
                    component.Properties = properties;
                }
            }
        }




        /// <summary>
        /// Determines whether the specified component exists in the provided AQL results (internal).
        /// </summary>
        /// <param name="aqlResultList">AQL results to search.</param>
        /// <param name="component">Component to locate.</param>
        /// <returns>True if an internal match is found; otherwise false.</returns>
        private static bool IsInternalConanComponent(List<AqlResult> aqlResultList, Component component)
        {
            string jfrogcomponentPath = $"{component.Name}/{component.Version}";
            if (aqlResultList.Exists(
                x => x.Path.Contains(jfrogcomponentPath, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.DebugFormat("IsInternalConanComponent(): Component [Name: {0}, Version: {1}] is internal,Found in JFrog repository with full name: {2}.", component.Name, component.Version, jfrogcomponentPath);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines the artifactory repository name and repository path for a Conan component based on AQL results.
        /// </summary>
        /// <param name="aqlResultList">List of AQL query results.</param>
        /// <param name="component">Component for which to locate a repository.</param>
        /// <param name="jfrogRepoPath">Output path within the JFrog repository when found.</param>
        /// <returns>Repository name or a sentinel if not found.</returns>
        public static string GetArtifactoryRepoName(List<AqlResult> aqlResultList, Component component, out string jfrogRepoPath)
        {
            Logger.DebugFormat("GetArtifactoryRepoName(): Starting identify JFrog repository details retrieval for component [Name: {0}, Version: {1}].", component.Name, component.Version);
            string jfrogcomponentPath = $"{component.Name}/{component.Version}";
            jfrogRepoPath = Dataconstant.JfrogRepoPathNotFound;
            var conanPackagePath = aqlResultList.FirstOrDefault(x => x.Path.Contains(jfrogcomponentPath) && x.Name.Contains("package.tgz"));
            if (conanPackagePath != null)
            {
                jfrogRepoPath = $"{conanPackagePath.Repo}/{conanPackagePath.Path}/{conanPackagePath.Name};";
            }
            var aqllist = aqlResultList.FindAll(x => x.Path.Contains(
                jfrogcomponentPath, StringComparison.OrdinalIgnoreCase));
            Logger.DebugFormat("GetArtifactoryRepoName(): Searching for component in JFrog repository with name: {0}.", jfrogcomponentPath);
            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqllist, component);

            return repoName;
        }
        #endregion

        #region Events
        #endregion
    }
}