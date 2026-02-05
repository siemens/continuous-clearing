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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier
{
    public class MavenProcessor(ICycloneDXBomParser cycloneDXBomParser, ISpdxBomParser spdxBomParser) : CycloneDXBomParser, IParser
    {
        #region Fields
        private const string FalseString = "false";
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        private static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };       
        private List<Component> listOfInternalComponents = new List<Component>();
        private readonly IEnvironmentHelper environmentHelper = new EnvironmentHelper();
        #endregion

        #region Properties
        #endregion

        #region Constructors
        #endregion

        #region Methods
        /// <summary>
        /// Parses Maven-related BOM files from input and constructs a combined CycloneDX BOM for comparison.
        /// </summary>
        /// <param name="appSettings">Application settings containing input folder and Maven configuration.</param>
        /// <param name="unSupportedBomList">Reference BOM to be populated with unsupported components discovered during parsing.</param>
        /// <returns>Constructed CycloneDX BOM.</returns>
        public Bom ParsePackageFile(CommonAppSettings appSettings, ref Bom unSupportedBomList)
        {
            Logger.Debug("ParsePackageFile():Starting to parse the package file for Maven components.");
            List<Component> componentsForBOM = new();
            List<Component> componentsToBOM = new();
            List<Component> ListOfComponents = new();
            Bom bom = new();
            int noOfExcludedComponents = 0;
            List<Dependency> dependenciesForBOM = new();
            List<string> configFiles;

            configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Maven, environmentHelper);
            List<string> listOfTemplateBomfilePaths = GetTemplateBomFilePaths(configFiles);
            ProcessBomFiles(configFiles, componentsForBOM, componentsToBOM, dependenciesForBOM, appSettings);

            string templateFilePath = SbomTemplate.GetFilePathForTemplate(listOfTemplateBomfilePaths);
            SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, componentsForBOM, appSettings.ProjectType);


            //checking Dev dependency
            DevDependencyIdentificationLogic(componentsForBOM, componentsToBOM, ref ListOfComponents);

            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile = componentsForBOM.Count + componentsToBOM.Count;

            int totalComponentsIdentified = BomCreator.bomKpiData.ComponentsinPackageLockJsonFile;

            //Removing if there are any other duplicates           
            componentsForBOM = ListOfComponents.Distinct(new ComponentEqualityComparer()).ToList();

            BomCreator.bomKpiData.DuplicateComponents = totalComponentsIdentified - componentsForBOM.Count;


            if (appSettings.SW360?.ExcludeComponents != null)
            {
                componentsForBOM = CommonHelper.RemoveExcludedComponents(componentsForBOM, appSettings.SW360?.ExcludeComponents, ref noOfExcludedComponents);
                BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents;
            }

            bom.Components = componentsForBOM;
            bom.Dependencies = dependenciesForBOM;
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
            BomCreator.bomKpiData.ComponentsInComparisonBOM = bom.Components.Count;            

            if (bom.Components != null)
            {
                AddSiemensDirectProperty(ref bom);
            }
            int totalUnsupportedComponents = ListUnsupportedComponentsForBom.Components.Count;
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += ListUnsupportedComponentsForBom.Components.Count;
            ListUnsupportedComponentsForBom.Components = ListUnsupportedComponentsForBom.Components.Distinct(new ComponentEqualityComparer()).ToList();
            BomCreator.bomKpiData.DuplicateComponents += totalUnsupportedComponents - ListUnsupportedComponentsForBom.Components.Count;
            AddSiemensDirectProperty(ref ListUnsupportedComponentsForBom);
            bom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(bom.Components, bom.Dependencies);
            ListUnsupportedComponentsForBom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(ListUnsupportedComponentsForBom.Components, ListUnsupportedComponentsForBom.Dependencies);
            unSupportedBomList.Components = ListUnsupportedComponentsForBom.Components;
            unSupportedBomList.Dependencies = ListUnsupportedComponentsForBom.Dependencies;
            RemoveTypeJarSuffix(bom);
            Logger.Debug("ParsePackageFile():Completed parsing the package file.\n");
            return bom;
        }

        /// <summary>
        /// Removes the ".jar" type suffix from component references and PURLs in the BOM.
        /// </summary>
        /// <param name="bom">BOM whose component and dependency references will be normalized.</param>
        private static void RemoveTypeJarSuffix(Bom bom)
        {
            const string suffix = Dataconstant.TypeJarSuffix;

            foreach (var component in bom?.Components ?? Enumerable.Empty<Component>())
            {
                component.BomRef = RemoveSuffix(component.BomRef, suffix);
                component.Purl = RemoveSuffix(component.Purl, suffix);
            }

            foreach (var dependency in bom?.Dependencies ?? Enumerable.Empty<Dependency>())
            {
                RemoveTypeJarSuffixFromDependency(dependency);
            }
        }

        /// <summary>
        /// Recursively removes the ".jar" type suffix from dependency refs.
        /// </summary>
        /// <param name="dependency">Dependency node to normalize.</param>
        
        public static void IdentifiedMavenComponents(string filePath, List<Component> components)
        {
            if (components == null || components.Count == 0)
            {
                // Log a message indicating no components were found
                Logger.DebugFormat("No components were found in the file: {0}", filePath);
                return;
            }
            // Build the table
            var logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine($"\n{LogHandlingHelper.LogSeparator}");
            logBuilder.AppendLine($" COMPONENTS FOUND IN FILE: {filePath}");
            logBuilder.AppendLine($"{LogHandlingHelper.LogSeparator}");
            logBuilder.AppendLine($"| {"Name",-40} | {"Version",-40} | {"PURL",-100} | {"DevDependent",-15} |");
            logBuilder.AppendLine($"{LogHandlingHelper.LogHeaderSeparator}");

            foreach (var component in components)
            {
                string devDependent = component.Properties?.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsDevelopment)?.Value ?? FalseString;
                logBuilder.AppendLine($"| {component.Name,-40} | {component.Version,-40} | {component.Purl,-100} | {devDependent,-15} |");
            }

            logBuilder.AppendLine($"{LogHandlingHelper.LogSeparator}");

            // Log the table
            Logger.Debug(logBuilder.ToString());
        }
        private static void RemoveTypeJarSuffixFromDependency(Dependency dependency)
        {
            const string suffix = Dataconstant.TypeJarSuffix;
            if (!string.IsNullOrEmpty(dependency.Ref) && dependency.Ref.EndsWith(suffix))
            {
                dependency.Ref = dependency.Ref[..^suffix.Length];
            }

            // Recursively process nested dependencies
            if (dependency.Dependencies != null && dependency.Dependencies.Count != 0)
            {
                foreach (var nestedDependency in dependency.Dependencies)
                {
                    RemoveTypeJarSuffixFromDependency(nestedDependency);
                }
            }
        }

        /// <summary>
        /// Processes each BOM file path and accumulates components and dependencies into provided lists.
        /// </summary>
        /// <param name="configFiles">List of configuration/BOM file paths to process.</param>
        /// <param name="componentsForBOM">Primary component list to populate.</param>
        /// <param name="componentsToBOM">Secondary component list used for dev dependency identification.</param>
        /// <param name="dependenciesForBOM">Dependency list to populate.</param>
        /// <param name="appSettings">Application settings used for SPDX validation and project type checks.</param>
        private void ProcessBomFiles(List<string> configFiles, List<Component> componentsForBOM, List<Component> componentsToBOM, List<Dependency> dependenciesForBOM, CommonAppSettings appSettings)
        {
            foreach (string filepath in configFiles)
            {
                if (!filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    Bom bomList;
                    if (filepath.EndsWith(FileConstant.SPDXFileExtension))
                    {
                        BomHelper.NamingConventionOfSPDXFile(filepath, appSettings);
                        Bom listUnsupportedComponents = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
                        bomList = _spdxBomParser.ParseSPDXBom(filepath);
                        IdentifiedMavenComponents(filepath, bomList.Components);
                        SpdxSbomHelper.CheckValidComponentsFromSpdxfile(bomList, appSettings.ProjectType, ref listUnsupportedComponents);
                        SpdxSbomHelper.AddSpdxSBomFileNameProperty(ref bomList, filepath);
                        SpdxSbomHelper.AddSpdxPropertysForUnsupportedComponents(listUnsupportedComponents.Components, filepath);
                        ListUnsupportedComponentsForBom.Components.AddRange(listUnsupportedComponents.Components);
                        ListUnsupportedComponentsForBom.Dependencies.AddRange(listUnsupportedComponents.Dependencies);
                    }
                    else
                    {
                        bomList = ParseCycloneDXBom(filepath);
                        if (bomList?.Components != null)
                        {
                            CheckValidComponentsForProjectType(bomList.Components, appSettings.ProjectType);
                        }
                        else
                        {
                            Logger.WarnFormat("No components found in the BoM file : {0}", filepath);
                            continue;
                        }
                    }

                    AddComponentsToBom(bomList, componentsForBOM, componentsToBOM, dependenciesForBOM);
                    IdentifiedMavenComponents(filepath, bomList.Components);
                }
            }
        }

        /// <summary>
        /// Adds components and dependencies from a parsed BOM into the accumulator lists.
        /// </summary>
        /// <param name="bomList">Parsed BOM whose contents should be merged.</param>
        /// <param name="componentsForBOM">Primary component accumulator.</param>
        /// <param name="componentsToBOM">Secondary component accumulator.</param>
        /// <param name="dependenciesForBOM">Dependency accumulator.</param>
        private static void AddComponentsToBom(Bom bomList, List<Component> componentsForBOM, List<Component> componentsToBOM, List<Dependency> dependenciesForBOM)
        {
            if (componentsForBOM.Count == 0)
            {
                componentsForBOM.AddRange(bomList.Components);
            }
            else
            {
                componentsToBOM.AddRange(bomList.Components);
            }

            if (bomList.Dependencies != null)
            {
                dependenciesForBOM.AddRange(bomList.Dependencies);
            }
        }

        /// <summary>
        /// Extracts SBOM template file paths from the provided file list.
        /// </summary>
        /// <param name="configFiles">List of file paths to inspect.</param>
        /// <returns>List of template SBOM file paths.</returns>
        private static List<string> GetTemplateBomFilePaths(List<string> configFiles)
        {
            List<string> listOfTemplateBomfilePaths = new();
            foreach (string filepath in configFiles)
            {
                if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    Logger.DebugFormat("GetTemplateBomFilePaths():Template BOM file detected: {0}", filepath);
                    listOfTemplateBomfilePaths.Add(filepath);
                }
            }
            return listOfTemplateBomfilePaths;
        }
        /// <summary>
        /// Removes a suffix from a string value if present.
        /// </summary>
        /// <param name="value">Input string to process.</param>
        /// <param name="suffix">Suffix to remove.</param>
        /// <returns>String without the suffix when present.</returns>
        private static string RemoveSuffix(string value, string suffix)
        {
            return !string.IsNullOrEmpty(value) && value.EndsWith(suffix)
                ? value[..^suffix.Length]
                : value;
        }

        /// <summary>
        /// Adds or updates the Siemens-specific direct-dependency property on each component in the BOM.
        /// </summary>
        /// <param name="bom">BOM whose components will be annotated.</param>
        public static void AddSiemensDirectProperty(ref Bom bom)
        {
            Logger.Debug("AddSiemensDirectProperty(): Starting to add SiemensDirect property to BOM components.");
            List<string> mavenDirectDependencies = new List<string>();
            mavenDirectDependencies.AddRange(bom.Dependencies?.Select(x => x.Ref).ToList() ?? new List<string>());
            var bomComponentsList = bom.Components;

            foreach (var component in bomComponentsList)
            {
                string siemensDirectValue = mavenDirectDependencies.Exists(x => x.Contains(component.Name) && x.Contains(component.Version))
                    ? "true"
                    : "false";
                Logger.DebugFormat("AddSiemensDirectProperty(): Component [Name: {0}, Version: {1}] is a direct dependency. Setting SiemensDirect property to {2}.", component.Name, component.Version, siemensDirectValue);
                component.Properties ??= new List<Property>();
                var properties = component.Properties;
                CommonHelper.RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_SiemensDirect, siemensDirectValue);
                component.Properties = properties;
            }
            Logger.Debug("AddSiemensDirectProperty(): Completed adding SiemensDirect property to BOM components.");
            bom.Components = bomComponentsList;
        }

        /// <summary>
        /// Evaluates components across two BOM sets to identify development-only dependencies.
        /// </summary>
        /// <param name="componentsForBOM">Components from the primary BOM.</param>
        /// <param name="componentsToBOM">Components from the secondary BOM.</param>
        /// <param name="ListOfComponents">Reference list that will be filled with annotated components.</param>
        public static void DevDependencyIdentificationLogic(List<Component> componentsForBOM, List<Component> componentsToBOM, ref List<Component> ListOfComponents)
        {

            List<Component> iterateBOM = componentsForBOM.Count > componentsToBOM.Count ? componentsForBOM : componentsToBOM;
            List<Component> checkBOM = componentsForBOM.Count < componentsToBOM.Count ? componentsForBOM : componentsToBOM;


            ListOfComponents = DevdependencyIdentification(ListOfComponents, iterateBOM, checkBOM);

        }

        /// <summary>
        /// Internal helper that marks components as development dependencies when they appear only in one BOM.
        /// </summary>
        /// <param name="ListOfComponents">Accumulator list to add processed components to.</param>
        /// <param name="iterateBOM">The BOM list iterated over.</param>
        /// <param name="checkBOM">The BOM list compared against to determine dev-only components.</param>
        /// <returns>Updated accumulator list.</returns>
        private static List<Component> DevdependencyIdentification(List<Component> ListOfComponents, List<Component> iterateBOM, List<Component> checkBOM)
        {
            foreach (var item in iterateBOM)
            {
                var scopeString = item.Scope?.ToString();
                if (!string.IsNullOrEmpty(scopeString) &&
                    scopeString.Equals("optional", StringComparison.OrdinalIgnoreCase))
                {
                    SetPropertiesforBOM(ref ListOfComponents, item, "true");
                    BomCreator.bomKpiData.DevDependentComponents++;
                    continue;
                }
                //check to see if the second list is empty(which means customer has only provided one bom file)no dev dependency will be identified here
                if (checkBOM.Count == 0)
                {
                    SetPropertiesforBOM(ref ListOfComponents, item, FalseString);
                }
                else if (checkBOM.Exists(x => x.Name == item.Name && x.Version == item.Version)) //check t see if both list has common elements
                {
                    SetPropertiesforBOM(ref ListOfComponents, item, FalseString);
                }
                else //incase one list has a component not present in another then it will be marked as Dev
                {
                    SetPropertiesforBOM(ref ListOfComponents, item, "true");

                    BomCreator.bomKpiData.DevDependentComponents++;
                }
            }

            return ListOfComponents;
        }

        /// <summary>
        /// Sets identifier and development properties for a component and appends it to the provided list.
        /// </summary>
        /// <param name="componentsToBOM">Reference to the component list to add to.</param>
        /// <param name="component">Component to annotate.</param>
        /// <param name="devValue">String value indicating development dependency ("true"/"false").</param>
        private static void SetPropertiesforBOM(ref List<Component> componentsToBOM, Component component, string devValue)
        {
            Property identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.Discovered };
            Property isDev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = devValue };
            Property spdxIdentifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.SpdxImport };

            if (CommonHelper.ComponentPropertyCheck(component, Dataconstant.Cdx_IdentifierType))
            {
                if (!CommonHelper.ComponentPropertyCheck(component, Dataconstant.Cdx_IsDevelopment))
                {
                    component.Properties.Add(isDev);
                    componentsToBOM.Add(component);
                }
                else
                {
                    componentsToBOM.Add(component);
                }
            }
            else
            {
                if (CommonHelper.ComponentPropertyCheck(component, Dataconstant.Cdx_SpdxFileName))
                {
                    component.Properties.Add(isDev);
                    component.Properties.Add(spdxIdentifierType);
                    componentsToBOM.Add(component);
                }
                else
                {
                    component.Properties = new List<Property>();
                    component.Properties.Add(isDev);
                    component.Properties.Add(identifierType);
                    componentsToBOM.Add(component);
                }

            }
        }

        /// <summary>
        /// Asynchronously enriches components with JFrog repository details (repo, file name, path, hashes).
        /// </summary>
        /// <param name="componentsForBOM">List of components to enrich.</param>
        /// <param name="appSettings">Application settings with repo configuration.</param>
        /// <param name="jFrogService">JFrog service used for queries.</param>
        /// <param name="bomhelper">BOM helper providing repository query helpers.</param>
        /// <returns>Asynchronously returns the modified components list.</returns>
        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM,
                                                                   CommonAppSettings appSettings,
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
                string jfrogpackageName = $"{component.Name}-{component.Version}{ApiConstant.MavenExtension}";
                Logger.DebugFormat("GetArtifactoryRepoName(): Searching for component in JFrog repository with name: {0}.", jfrogpackageName);
                var hashes = aqlResultList.FirstOrDefault(x => x.Name == jfrogpackageName);

                AqlResult finalRepoData = GetJfrogArtifactoryRepoDetials(aqlResultList, component, bomhelper, out string jfrogRepoPath);
                Property siemensfileNameProp = new() { Name = Dataconstant.Cdx_Siemensfilename, Value = finalRepoData?.Name ?? Dataconstant.PackageNameNotFoundInJfrog };
                Property jfrogRepoPathProp = new() { Name = Dataconstant.Cdx_JfrogRepoPath, Value = jfrogRepoPath };
                Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = finalRepoData?.Repo };

                Component componentVal = component;

                // Extract KPI update logic to helper method
                UpdateKpiDataBasedOnRepo(artifactoryrepo.Value, appSettings);

                // Use common helper to set component properties and hashes
                CommonHelper.SetComponentPropertiesAndHashes(componentVal, artifactoryrepo, projectType, siemensfileNameProp, jfrogRepoPathProp, hashes);

                modifiedBOM.Add(componentVal);
            }
            LogHandlingHelper.IdentifierComponentsData(componentsForBOM, listOfInternalComponents);
            Logger.Debug("GetJfrogRepoDetailsOfAComponent():Completed retrieving JFrog repository details for components.\n");
            return modifiedBOM;
        }

        /// <summary>
        /// Asynchronously identifies internal Maven components by querying configured internal repositories.
        /// </summary>
        /// <param name="componentData">Component identification structure containing comparison BOM data.</param>
        /// <param name="appSettings">Application settings with Artifactory internal repos.</param>
        /// <param name="jFrogService">JFrog service used to query AQL results.</param>
        /// <param name="bomhelper">BOM helper utilities.</param>
        /// <returns>Asynchronously returns updated component identification data with internal components set.</returns>
        public async Task<ComponentIdentification> IdentificationOfInternalComponents(
           ComponentIdentification componentData, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            Logger.Debug("IdentificationOfInternalComponents(): Starting identification of internal components.");

            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList = await bomhelper.GetListOfComponentsFromRepo(appSettings.Maven.Artifactory.InternalRepos, jFrogService);

            var inputIterationList = componentData.comparisonBOMData;

            // Use the common helper method
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(
                inputIterationList,
                component => IsInternalMavenComponent(aqlResultList, component, bomhelper));

            // update the comparison bom data
            componentData.comparisonBOMData = processedComponents;
            componentData.internalComponents = internalComponents;
            listOfInternalComponents = internalComponents;
            Logger.DebugFormat("IdentificationOfInternalComponents(): identified internal components:{0}.", internalComponents.Count);
            Logger.Debug("IdentificationOfInternalComponents(): Completed identification of internal components.\n");
            return componentData;
        }
        /// <summary>
        /// Updates KPI counters depending on which repository a component was found in.
        /// </summary>
        /// <param name="repoValue">Repository name discovered for the component.</param>
        /// <param name="appSettings">Application settings used to map repo categories.</param>
        private static void UpdateKpiDataBasedOnRepo(string repoValue, CommonAppSettings appSettings)
        {
            if (repoValue == appSettings.Maven.DevDepRepo)
            {
                BomCreator.bomKpiData.DevdependencyComponents++;
                return;
            }

            if (appSettings.Maven.Artifactory.ThirdPartyRepos != null)
            {
                foreach (var thirdPartyRepo in appSettings.Maven.Artifactory.ThirdPartyRepos)
                {
                    if (repoValue == thirdPartyRepo.Name)
                    {
                        BomCreator.bomKpiData.ThirdPartyRepoComponents++;
                        return;
                    }
                }
            }

            if (repoValue == appSettings.Maven.ReleaseRepo)
            {
                BomCreator.bomKpiData.ReleaseRepoComponents++;
                return;
            }

            if (repoValue == Dataconstant.NotFoundInJFrog || repoValue == "")
            {
                BomCreator.bomKpiData.UnofficialComponents++;
            }
        }

        /// <summary>
        /// Determines whether a Maven component is internal by inspecting AQL results and fallbacks.
        /// </summary>
        /// <param name="aqlResultList">List of AQL results to search.</param>
        /// <param name="component">Component to check.</param>
        /// <param name="bomHelper">BOM helper for generating full component names.</param>
        /// <returns>True if component is found in internal results; otherwise false.</returns>
        private static bool IsInternalMavenComponent(List<AqlResult> aqlResultList, Component component, IBomHelper bomHelper)
        {
            string jfrogcomponentName = $"{component.Name}-{component.Version}";
            if (aqlResultList.Exists(x => x.Name.Contains(jfrogcomponentName, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.DebugFormat("IsInternalMavenComponent(): Component [Name: {0}, Version: {1}] is internal,Found in JFrog repository with full name: {2}.", component.Name, component.Version, jfrogcomponentName);
                return true;
            }

            string fullName = bomHelper.GetFullNameOfComponent(component);
            string fullNameVersion = $"{fullName}-{component.Version}";
            if (!fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase) && aqlResultList.Exists(
                x => x.Name.Contains(fullNameVersion, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.DebugFormat("IsInternalMavenComponent(): Component [Name: {0}, Version: {1}] is internal,Found in JFrog repository with full name: {2}.", component.Name, component.Version, fullNameVersion);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Locates JFrog AQL entry for a Maven component and returns associated repo details and path.
        /// </summary>
        /// <param name="aqlResultList">AQL results to search.</param>
        /// <param name="component">Component for which to locate artifact.</param>
        /// <param name="bomHelper">BOM helper for full-name resolution fallback.</param>
        /// <param name="jfrogRepoPath">Outputs the repo path when found.</param>
        /// <returns>Found <see cref="AqlResult"/> or an empty item when none found.</returns>
        private static AqlResult GetJfrogArtifactoryRepoDetials(List<AqlResult> aqlResultList,
                                                                Component component,
                                                                IBomHelper bomHelper,
                                                                out string jfrogRepoPath)
        {
            AqlResult aqlResult = new AqlResult();
            jfrogRepoPath = Dataconstant.JfrogRepoPathNotFound;
            string jfrogcomponentName = $"{component.Name}-{component.Version}.jar";
            Logger.DebugFormat("GetJfrogArtifactoryRepoDetials(): Searching for component in JFrog repository with name: {0}.", jfrogcomponentName);
            var aqlResults = aqlResultList.FindAll(x => x.Name.Equals(
                jfrogcomponentName, StringComparison.OrdinalIgnoreCase));

            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);

            if (repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                string fullName = bomHelper.GetFullNameOfComponent(component);
                string fullNameVersion = $"{fullName}-{component.Version}.jar";
                Logger.DebugFormat("GetJfrogArtifactoryRepoDetials(): Searching for component in JFrog repository with name: {0}.", fullNameVersion);
                if (!fullNameVersion.Equals(jfrogcomponentName, StringComparison.OrdinalIgnoreCase))
                {
                    aqlResults = aqlResultList.FindAll(x => x.Name.Equals(
                    fullNameVersion, StringComparison.OrdinalIgnoreCase));

                    repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);
                }
            }

            // Forming Jfrog repo Path
            if (!repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                aqlResult = aqlResults.FirstOrDefault(x => x.Repo.Equals(repoName));
                jfrogRepoPath = GetJfrogRepoPath(aqlResult);
            }
            Logger.DebugFormat("GetJfrogArtifactoryRepoDetials(): JFrog repository path: {0}.", jfrogRepoPath);
            if (aqlResult != null)
            {
                aqlResult.Repo ??= repoName;
            }
            return aqlResult;
        }

        /// <summary>
        /// Builds a JFrog repository path string from an AQL result entry.
        /// </summary>
        /// <param name="aqlResult">AQL result entry to format.</param>
        /// <returns>Formatted repo path string.</returns>
        private static string GetJfrogRepoPath(AqlResult aqlResult)
        {
            if (string.IsNullOrEmpty(aqlResult.Path) || aqlResult.Path.Equals("."))
            {
                return $"{aqlResult.Repo}/{aqlResult.Name}";
            }

            return $"{aqlResult.Repo}/{aqlResult.Path}/{aqlResult.Name}";
        }
        #endregion

        #region Events
        #endregion
    }
}
