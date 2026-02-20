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
    /// Parses the NPM Packages
    /// </summary>
    public class NpmProcessor(ICycloneDXBomParser cycloneDXBomParser, ISpdxBomParser spdxBomParser) : CycloneDXBomParser, IParser
    {
        #region Fields
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        private readonly static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
        private const string Bundled = "bundled";
        private const string Dependencies = "dependencies";
        private const string Dev = "dev";
        private const string DevOptional = "devOptional";
        private const string Version = "version";
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
        private const string Requires = "requires";
        private const string Name = "name";
        private const string FalseString = "false";

        private List<Component> listOfInternalComponents = new List<Component>();
        private readonly IEnvironmentHelper environmentHelper = new EnvironmentHelper();
        #endregion
        #region Properties
        #endregion

        #region Constructors
        #endregion

        #region Methods
        public Bom ParsePackageFile(CommonAppSettings appSettings, ref Bom unSupportedBomList)
        {
            Logger.Debug("ParsePackageFile():Starting to parse the package file for NPM components.");
            List<Component> componentsForBOM = new List<Component>();
            List<Component> ListofComponentsFromLockFile = new List<Component>();
            List<Dependency> ListofDependenciesFromLockFile = new List<Dependency>();
            Bom bom = new Bom();
            List<Dependency> dependencies = new List<Dependency>();
            int totalComponentsIdentified = 0;
            int totalUnsupportedComponentsIdentified = 0;
            ParsingInputFileForBOM(appSettings, ref componentsForBOM, ref bom, ref dependencies, ref ListofComponentsFromLockFile, ref ListofDependenciesFromLockFile);
            totalComponentsIdentified = componentsForBOM.Count;
            totalUnsupportedComponentsIdentified = ListUnsupportedComponentsForBom.Components.Count;
            componentsForBOM = BomHelper.GetExcludedComponentsList(componentsForBOM, Dataconstant.PurlCheck()["NPM"], appSettings?.ProjectType);
            componentsForBOM = componentsForBOM.Distinct(new ComponentEqualityComparer()).ToList();
            ListUnsupportedComponentsForBom.Components = ListUnsupportedComponentsForBom.Components.Distinct(new ComponentEqualityComparer()).ToList();
            BomCreator.bomKpiData.DuplicateComponents = totalComponentsIdentified - componentsForBOM.Count;
            BomCreator.bomKpiData.DuplicateComponents += totalUnsupportedComponentsIdentified - ListUnsupportedComponentsForBom.Components.Count;
            var componentsWithMultipleVersions = componentsForBOM.GroupBy(s => s.Name)
                              .Where(g => g.Count() > 1).SelectMany(g => g).ToList();

            if (componentsWithMultipleVersions.Count != 0)
            {
                CreateFileForMultipleVersions(componentsWithMultipleVersions, appSettings);
            }
            bom.Components = componentsForBOM;
            bom.Dependencies = dependencies;
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
            bom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(bom.Components, bom.Dependencies);
            CommonHelper.AddSiemensDirectProperty(ref bom);
            ListUnsupportedComponentsForBom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(ListUnsupportedComponentsForBom.Components, ListUnsupportedComponentsForBom.Dependencies);
            unSupportedBomList.Components = ListUnsupportedComponentsForBom.Components;
            unSupportedBomList.Dependencies = ListUnsupportedComponentsForBom.Dependencies;
            Logger.Debug("ParsePackageFile():Completed parsing the package file for NPM components.\n");
            return bom;
        }
        /// <summary>
        /// Parses a package-lock.json file and returns CycloneDX components derived from it.
        /// </summary>
        /// <param name="filepath">Path to the package-lock.json file.</param>
        /// <param name="appSettings">Application settings used for exclusions and KPIs.</param>
        /// <returns>List of components parsed from the package-lock.json file.</returns>

        public static List<Component> ParsePackageLockJson(string filepath, CommonAppSettings appSettings)
        {
            List<BundledComponents> bundledComponents = new List<BundledComponents>();
            List<Component> lstComponentForBOM = new List<Component>();
            int noOfDevDependent = 0;
            int noOfExcludedComponents = 0;
            try
            {
                string jsonContent = File.ReadAllText(filepath);
                var jsonDeserialized = JObject.Parse(jsonContent);
                var dependencies = jsonDeserialized[Dependencies];

                // multi level dependency check
                if (dependencies?.Children() != null)
                {
                    List<JToken> directDependenciesList = GetDirectDependenciesList(filepath);
                    IEnumerable<JProperty> depencyComponentList = dependencies.Children().OfType<JProperty>();
                    GetComponentsForBom(filepath, appSettings, ref bundledComponents, ref lstComponentForBOM, ref noOfDevDependent, depencyComponentList, directDependenciesList);
                }

                // the below logic for angular 16+version due to package-lock.json file format change
                if (dependencies == null)
                {
                    var pacakages = jsonDeserialized["packages"];
                    if (pacakages?.Children() != null)
                    {
                        IEnumerable<JProperty> depencyComponentList = pacakages.Children().OfType<JProperty>();
                        GetPackagesForBom(filepath, ref bundledComponents, ref lstComponentForBOM,
                            ref noOfDevDependent, depencyComponentList);
                    }
                }

                if (appSettings?.SW360?.ExcludeComponents != null)
                {
                    lstComponentForBOM = CommonHelper.RemoveExcludedComponents(lstComponentForBOM, appSettings.SW360.ExcludeComponents, ref noOfExcludedComponents);
                    BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents;

                }
                BomCreator.bomKpiData.DevDependentComponents += noOfDevDependent;
                BomCreator.bomKpiData.BundledComponents += bundledComponents.Count;
            }
            catch (JsonReaderException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("JsonReaderException", "ParsePackageLockJson()", ex, $"File Path: {filepath}");
                Logger.Error(string.Format("Failed to parse JSON file. File Path: {0}. Details: {1}", filepath, ex.Message), ex);
                Environment.ExitCode = -1;
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("IOException", "ParsePackageLockJson()", ex, $"File Path: {filepath}");
                Logger.Error(string.Format("IO error occurred while processing the file. File Path: {0}. Details: {1}", filepath, ex.Message), ex);
                Environment.ExitCode = -1;
            }
            catch (SecurityException ex)
            {
                Environment.ExitCode = -1;
                LogHandlingHelper.ExceptionErrorHandling("SecurityException", "ParsePackageLockJson()", ex, $"File Path: {filepath}");
                Logger.Error(string.Format("Security error occurred while accessing the file. File Path: {0}. Details: {1}", filepath, ex.Message), ex);
            }

            return lstComponentForBOM;
        }

        /// <summary>
        /// Returns a combined list of top-level dependencies and devDependencies from a package-lock.json file.
        /// </summary>
        /// <param name="filepath">Path to the package-lock.json file.</param>
        /// <returns>List of JToken entries representing direct dependencies.</returns>
        private static List<JToken> GetDirectDependenciesList(string filepath)
        {
            string jsonContent = File.ReadAllText(filepath);
            var jsonDeserialized = JObject.Parse(jsonContent);
            List<JToken> dependencies = jsonDeserialized[Dependencies]?.ToList() ?? new List<JToken>();
            List<JToken> devDependencies = jsonDeserialized["devDependencies"]?.ToList() ?? new List<JToken>();
            List<JToken> directDependencies = new List<JToken>();
            directDependencies.AddRange(dependencies);
            directDependencies.AddRange(devDependencies);
            return directDependencies;
        }

        /// <summary>
        /// Writes a file that documents components which appear with multiple versions.
        /// </summary>
        /// <param name="componentsWithMultipleVersions">List of components that have multiple versions detected.</param>
        /// <param name="appSettings">Application settings used to determine output paths.</param>
        private static void CreateFileForMultipleVersions(List<Component> componentsWithMultipleVersions, CommonAppSettings appSettings)
        {
            MultipleVersions multipleVersions = new MultipleVersions();
            FileOperations fileOperations = new FileOperations();
            string defaultProjectName = CommonIdentiferHelper.GetDefaultProjectName(appSettings);
            string bomFullPath = $"{appSettings.Directory.OutputFolder}\\{defaultProjectName}_Bom.cdx.json";
            string filePath = $"{appSettings.Directory.OutputFolder}\\{defaultProjectName}_{FileConstant.multipleversionsFileName}";
            if (!File.Exists(filePath))
            {
                multipleVersions.Npm = new List<MultipleVersionValues>();
                foreach (var npmpackage in componentsWithMultipleVersions)
                {
                    npmpackage.Description = !string.IsNullOrEmpty(bomFullPath) ? bomFullPath : npmpackage.Description;
                    MultipleVersionValues jsonComponents = new MultipleVersionValues();
                    jsonComponents.ComponentName = npmpackage.Name;
                    jsonComponents.ComponentVersion = npmpackage.Version;
                    jsonComponents.PackageFoundIn = npmpackage.Description;
                    multipleVersions.Npm.Add(jsonComponents);
                }
                fileOperations.WriteContentToMultipleVersionsFile(multipleVersions, appSettings.Directory.OutputFolder, FileConstant.multipleversionsFileName, defaultProjectName);
                Logger.Warn($"\nTotal Multiple versions detected {multipleVersions.Npm.Count} and details can be found at {filePath}\n");
            }
            else
            {
                string json = File.ReadAllText(filePath);
                MultipleVersions myDeserializedClass = JsonConvert.DeserializeObject<MultipleVersions>(json);
                List<MultipleVersionValues> npmComponents = new List<MultipleVersionValues>();
                foreach (var npmpackage in componentsWithMultipleVersions)
                {
                    MultipleVersionValues jsonComponents = new MultipleVersionValues();
                    jsonComponents.ComponentName = npmpackage.Name;
                    jsonComponents.ComponentVersion = npmpackage.Version;
                    jsonComponents.PackageFoundIn = npmpackage.Description;

                    npmComponents.Add(jsonComponents);
                }
                myDeserializedClass.Npm = npmComponents;

                fileOperations.WriteContentToMultipleVersionsFile(myDeserializedClass, appSettings.Directory.OutputFolder, FileConstant.multipleversionsFileName, defaultProjectName);
                Logger.Warn($"\nTotal Multiple versions detected {npmComponents.Count} and details can be found at {filePath}\n");
            }
        }


        /// <summary>
        /// Extracts components from the legacy "packages" section of certain package-lock.json versions.
        /// </summary>
        /// <param name="filepath">Source file path.</param>
        /// <param name="bundledComponents">Accumulator for bundled components.</param>
        /// <param name="lstComponentForBOM">Accumulator for components to include in BOM.</param>
        /// <param name="noOfDevDependent">Reference counter for dev dependencies.</param>
        /// <param name="depencyComponentList">Enumerable of JProperty dependency entries.</param>
        private static void GetPackagesForBom(string filepath, ref List<BundledComponents> bundledComponents, ref List<Component> lstComponentForBOM, ref int noOfDevDependent, IEnumerable<JProperty> depencyComponentList)
        {
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += depencyComponentList.Count();
            var property2 = depencyComponentList.ToList()[0];
            var parsedContent = JObject.Parse(Convert.ToString(property2.Value));
            List<JToken> dep = parsedContent["dependencies"]?.ToList() ?? new List<JToken>();
            List<JToken> devDep = parsedContent["devDependencies"]?.ToList() ?? new List<JToken>();
            List<JToken> directDependencies = new List<JToken>();
            directDependencies.AddRange(dep);
            directDependencies.AddRange(devDep);

            foreach (JProperty prop in depencyComponentList.Skip(1))
            {
                Property isdev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = FalseString };
                if (string.IsNullOrEmpty(prop.Name))
                {
                    BomCreator.bomKpiData.ComponentsinPackageLockJsonFile--;
                    continue;
                }

                Component components = new Component() { Manufacturer = new OrganizationalEntity() };
                var properties = JObject.Parse(Convert.ToString(prop.Value));

                // dev components are not ignored and added as a part of SBOM 
                // If package section has Dev or DevOptional as true , considering it as Dev Component
                if (IsDevDependency(prop.Value[Dev], ref noOfDevDependent) || IsDevDependency(prop.Value[DevOptional], ref noOfDevDependent))
                {
                    isdev.Value = "true";
                }

                string folderPath = CommonHelper.TrimEndOfString(filepath, $"\\{FileConstant.PackageLockFileName}");
                string packageName = GetPackageName(properties, prop);
                string bomrefName = packageName;
                string componentName = packageName.StartsWith('@') ? packageName.Replace("@", "%40") : packageName;

                SetComponentGroupAndName(components, packageName);

                components.Type = Component.Classification.Library;
                components.Description = folderPath;
                components.Version = Convert.ToString(properties[Version]);
                components.Manufacturer.BomRef = prop.Value[Dependencies]?.ToString();
                components.Purl = $"{ApiConstant.NPMExternalID}{componentName}@{components.Version}";
                components.BomRef = $"{ApiConstant.NPMExternalID}{bomrefName}@{components.Version}";

                CheckAndAddToBundleComponents(bundledComponents, prop, components);
                string isDirect = GetIsDirect(directDependencies, prop);
                Property siemensDirect = new Property() { Name = Dataconstant.Cdx_SiemensDirect, Value = isDirect };
                components.Properties = new List<Property>();
                components.Properties.Add(isdev);
                components.Properties.Add(siemensDirect);
                lstComponentForBOM.Add(components);
                lstComponentForBOM = RemoveBundledComponentFromList(bundledComponents, lstComponentForBOM);
            }
        }

        /// <summary>
        /// Sets the Group and Name properties on a component from an NPM package identifier.
        /// </summary>
        /// <param name="component">Component to update.</param>
        /// <param name="packageName">Package identifier (may include scope).</param>
        private static void SetComponentGroupAndName(Component component, string packageName)
        {
            if (packageName.Contains('@'))
            {
                component.Group = packageName.Split('/')[0];
                component.Name = packageName.Split('/')[1];
            }
            else
            {
                component.Name = packageName;
            }
        }
        /// <summary>
        /// Resolves the package name from parsed properties or from the node_modules path.
        /// </summary>
        /// <param name="properties">JObject of package properties.</param>
        /// <param name="prop">JProperty representing the package entry.</param>
        /// <returns>Package name string.</returns>
        private static string GetPackageName(JObject properties, JProperty prop)
        {
            string packageName;
            if (properties[Name] != null)
            {
                packageName = Convert.ToString(properties[Name]);
            }
            else
            {
                packageName = CommonHelper.GetSubstringOfLastOccurance(prop.Name, $"node_modules/");
            }
            return packageName;
        }
        /// <summary>
        /// Determines if a given component entry is a direct dependency.
        /// </summary>
        /// <param name="directDependencies">List of direct dependency tokens.</param>
        /// <param name="prop">JProperty for the component to test.</param>
        /// <returns>"true" if direct; otherwise "false".</returns>
        public static string GetIsDirect(List<JToken> directDependencies, JProperty prop)
        {
            string subvalue = CommonHelper.GetSubstringOfLastOccurance(prop.Name, $"node_modules/");
            foreach (var item in directDependencies)
            {
                string value = Convert.ToString(item) ?? string.Empty;
                if (value.Contains(subvalue))
                {
                    return "true";
                }
            }

            return "false";
        }
        /// <summary>
        /// Adds a component to bundled components list when the "bundled" flag is present.
        /// </summary>
        /// <param name="bundledComponents">Accumulator for bundled components.</param>
        /// <param name="prop">JProperty representing the package entry.</param>
        /// <param name="components">Component instance to add.</param>
        private static void CheckAndAddToBundleComponents(List<BundledComponents> bundledComponents, JProperty prop, Component components)
        {
            if (prop.Value[Bundled] != null && (!bundledComponents.Any(x => x.Name == components.Name && x.Version.Equals(components.Version, StringComparison.OrdinalIgnoreCase))))
            {
                BundledComponents component = new() { Name = components.Name, Version = components.Version };
                bundledComponents.Add(component);
            }
        }


        /// <summary>
        /// Recursively extracts components from nested dependency structures in package-lock.json.
        /// </summary>
        /// <param name="filepath">Source package-lock.json path.</param>
        /// <param name="appSettings">Application settings for exclusion checks.</param>
        /// <param name="bundledComponents">Accumulator for bundled components.</param>
        /// <param name="lstComponentForBOM">Accumulator for components to add to the BOM.</param>
        /// <param name="noOfDevDependent">Reference dev-dependency counter.</param>
        /// <param name="depencyComponentList">Enumerable of dependency JProperty entries.</param>
        /// <param name="directDependenciesList">List of tokens representing direct dependencies.</param>
        private static void GetComponentsForBom(string filepath, CommonAppSettings appSettings,
            ref List<BundledComponents> bundledComponents, ref List<Component> lstComponentForBOM,
            ref int noOfDevDependent, IEnumerable<JProperty> depencyComponentList, List<JToken> directDependenciesList)
        {
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += depencyComponentList.Count();

            foreach (JProperty prop in depencyComponentList)
            {
                Component components = new Component() { Manufacturer = new OrganizationalEntity() };
                Property isdev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = FalseString };

                var properties = JObject.Parse(Convert.ToString(prop.Value));

                // dev components are not ignored and added as a part of SBOM 
                // If package section has Dev or DevOptional as true , considering it as Dev Component 
                if (IsDevDependency(prop.Value[Dev], ref noOfDevDependent) || IsDevDependency(prop.Value[DevOptional], ref noOfDevDependent))
                {
                    isdev.Value = "true";
                }

                IEnumerable<JProperty> subDependencyComponentList = prop.Value[Dependencies]?.OfType<JProperty>();
                if (subDependencyComponentList != null)
                {
                    GetComponentsForBom(filepath, appSettings, ref bundledComponents, ref lstComponentForBOM,
                                        ref noOfDevDependent, subDependencyComponentList, directDependenciesList);
                }

                GetBundledComponents(prop.Value[Dependencies], ref bundledComponents);
                string bomrefName = prop.Name;
                string componentName = prop.Name.StartsWith('@') ? prop.Name.Replace("@", "%40") : prop.Name;
                string folderPath = CommonHelper.TrimEndOfString(filepath, $"\\{FileConstant.PackageLockFileName}");

                if (prop.Name.Contains('@'))
                {
                    components.Group = prop.Name.Split('/')[0];
                    components.Name = prop.Name.Split('/')[1];

                }
                else
                {
                    components.Name = prop.Name;
                }

                components.Description = folderPath;
                components.Version = Convert.ToString(properties[Version]);
                components.Manufacturer.BomRef = prop.Value[Requires]?.ToString();
                components.Purl = $"{ApiConstant.NPMExternalID}{componentName}@{components.Version}";
                components.BomRef = $"{ApiConstant.NPMExternalID}{bomrefName}@{components.Version}";
                components.Type = Component.Classification.Library;
                string isDirect = GetIsDirect(directDependenciesList, prop);
                Property siemensDirect = new Property() { Name = Dataconstant.Cdx_SiemensDirect, Value = isDirect };
                components.Properties = new List<Property>();
                components.Properties.Add(isdev);
                components.Properties.Add(siemensDirect);
                lstComponentForBOM.Add(components);
                lstComponentForBOM = RemoveBundledComponentFromList(bundledComponents, lstComponentForBOM);
            }
        }

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(
            ComponentIdentification componentData, CommonAppSettings appSettings,
            IJFrogService jFrogService, IBomHelper bomhelper)
        {
            Logger.Debug("IdentificationOfInternalComponents(): Starting identification of internal components.");
            // get the  component list from Jfrog for given repo
            List<AqlResult> aqlResultList =
                await bomhelper.GetNpmListOfComponentsFromRepo(appSettings.Npm.Artifactory.InternalRepos, jFrogService);

            var inputIterationList = componentData.comparisonBOMData;

            // Use the common helper method
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(
                inputIterationList,
                component => IsInternalNpmComponent(aqlResultList, component, bomhelper));

            // update the comparison bom data
            componentData.comparisonBOMData = processedComponents;
            componentData.internalComponents = internalComponents;
            listOfInternalComponents = internalComponents;
            Logger.DebugFormat("IdentificationOfInternalComponents(): identified internal components:{0}.", internalComponents.Count);
            Logger.Debug("IdentificationOfInternalComponents(): Completed identification of internal components.\n");
            return componentData;
        }

        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM,
            CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            Logger.Debug("GetJfrogRepoDetailsOfAComponent():Starting to retrieve JFrog repository details for components.\n");
            // get the  component list from Jfrog for given repo + internal repo
            string[] repoList = CommonHelper.GetRepoList(appSettings);
            List<AqlResult> aqlResultList = await bomhelper.GetNpmListOfComponentsFromRepo(repoList, jFrogService);

            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                var processedComponent = ProcessComponent(component, aqlResultList, bomhelper, appSettings, projectType);
                modifiedBOM.Add(processedComponent);
            }
            LogHandlingHelper.IdentifierComponentsData(componentsForBOM, listOfInternalComponents);

            Logger.Debug("GetJfrogRepoDetailsOfAComponent():Completed retrieving JFrog repository details for components.\n");
            return modifiedBOM;
        }

        private static Component ProcessComponent(Component component, List<AqlResult> aqlResultList, IBomHelper bomhelper, CommonAppSettings appSettings, Property projectType)
        {
            string jfrogpackageName = bomhelper.GetFullNameOfComponent(component);

            var hashes = aqlResultList.FirstOrDefault(x => x.Properties.Any(p => p.Key == "npm.name" && p.Value == jfrogpackageName) && x.Properties.Any(p => p.Key == "npm.version" && p.Value == component.Version));
            string jfrogRepoPath = string.Empty;
            AqlResult finalRepoData = GetJfrogArtifactoryRepoDetials(aqlResultList, component, bomhelper, out jfrogRepoPath);
            Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = finalRepoData.Repo };

            Property siemensfileNameProp = new() { Name = Dataconstant.Cdx_Siemensfilename, Value = finalRepoData.Name ?? Dataconstant.PackageNameNotFoundInJfrog };
            Property jfrogRepoPathProp = new() { Name = Dataconstant.Cdx_JfrogRepoPath, Value = jfrogRepoPath };
            Component componentVal = component;

            UpdateKpiDataBasedOnRepo(artifactoryrepo.Value, appSettings);

            // Use common helper to set component properties and hashes
            CommonHelper.SetComponentPropertiesAndHashes(componentVal, artifactoryrepo, projectType, siemensfileNameProp, jfrogRepoPathProp, hashes);

            return componentVal;
        }

        private static void UpdateKpiDataBasedOnRepo(string repoValue, CommonAppSettings appSettings)
        {
            if (repoValue == appSettings.Npm.DevDepRepo)
            {
                BomCreator.bomKpiData.DevdependencyComponents++;
            }

            if (appSettings.Npm.Artifactory.ThirdPartyRepos != null)
            {
                foreach (var thirdPartyRepo in appSettings.Npm.Artifactory.ThirdPartyRepos)
                {
                    if (repoValue == thirdPartyRepo.Name)
                    {
                        BomCreator.bomKpiData.ThirdPartyRepoComponents++;
                        break;
                    }
                }
            }

            if (repoValue == appSettings.Npm.ReleaseRepo)
            {
                BomCreator.bomKpiData.ReleaseRepoComponents++;
            }

            if (repoValue == Dataconstant.NotFoundInJFrog || repoValue == "")
            {
                BomCreator.bomKpiData.UnofficialComponents++;
            }
        }

        public static Bom RemoveExcludedComponents(CommonAppSettings appSettings, Bom cycloneDXBOM)
        {
            return CommonHelper.RemoveExcludedComponentsFromBom(appSettings, cycloneDXBOM,
                noOfExcludedComponents => BomCreator.bomKpiData.ComponentsExcludedSW360 += noOfExcludedComponents);
        }

        /// <summary>
        /// Scans the input folder for NPM-related files and parses them into components and dependencies.
        /// </summary>
        /// <param name="appSettings">Application settings containing input folder and NPM config.</param>
        /// <param name="componentsForBOM">Reference list to populate with discovered components.</param>
        /// <param name="bom">Reference BOM that may be filled when parsing CycloneDX/SPDX files.</param>
        /// <param name="dependencies">Reference dependency list to populate.</param>
        private void ParsingInputFileForBOM(CommonAppSettings appSettings, ref List<Component> componentsForBOM, ref Bom bom, ref List<Dependency> dependencies, ref List<Component> ListofComponentsFromLockFile, ref List<Dependency> ListofDependenciesFromLockFile)
        {
            List<string> configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Npm, environmentHelper);
            List<string> listOfTemplateBomfilePaths = new List<string>();
            Bom cdxGenBomData = GetCdxGenBomData(configFiles, appSettings);
            foreach (string filepath in configFiles)
            {
                if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    Logger.DebugFormat("ParsingInputFileForBOM():Template BOM file detected: {0}", filepath);
                    listOfTemplateBomfilePaths.Add(filepath);
                    continue;
                }

                Logger.DebugFormat("ParsingInputFileForBOM():FileName: {0}", filepath);
                ProcessFileBasedOnType(filepath, appSettings, ref componentsForBOM, ref bom, ref dependencies, ref ListofComponentsFromLockFile, ref ListofDependenciesFromLockFile);
            }
            CommonHelper.EnrichCdxGenforPackagefilesData(
                ref ListofComponentsFromLockFile,
                ref ListofDependenciesFromLockFile,
                ref componentsForBOM,
                ref dependencies,
                cdxGenBomData);

            string templateFilePath = SbomTemplate.GetFilePathForTemplate(listOfTemplateBomfilePaths);
            SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, componentsForBOM, appSettings.ProjectType);
        }
        private Bom GetCdxGenBomData(List<string> configFiles, CommonAppSettings appSettings)
        {
            return CommonIdentiferHelper.GetCdxGenBomData(configFiles, appSettings, _cycloneDXBomParser.ParseCycloneDXBom);
        }

        /// <summary>
        /// Dispatches processing based on the file type (CycloneDX, SPDX or package file).
        /// </summary>
        /// <param name="filepath">Path of the file to process.</param>
        /// <param name="appSettings">Application settings.</param>
        /// <param name="componentsForBOM">Reference component list.</param>
        /// <param name="bom">Reference BOM to update.</param>
        /// <param name="dependencies">Reference dependency list.</param>
        private void ProcessFileBasedOnType(string filepath, CommonAppSettings appSettings, ref List<Component> componentsForBOM, ref Bom bom, ref List<Dependency> dependencies, ref List<Component> ListofComponentsFromLockFile, ref List<Dependency> ListofDependenciesFromLockFile)

        {
            if (filepath.EndsWith(FileConstant.CycloneDXFileExtension) || filepath.EndsWith(FileConstant.DependencyFileExtension))
            {
                ProcessCycloneDXFile(filepath, appSettings, ref componentsForBOM, ref bom, ref dependencies);
            }
            else if (filepath.EndsWith(FileConstant.SPDXFileExtension))
            {
                ProcessSPDXFile(filepath, appSettings, ref componentsForBOM, ref bom, ref dependencies);
            }
            else
            {
                ProcessPackageFile(filepath, appSettings, ref ListofComponentsFromLockFile, ref ListofDependenciesFromLockFile);
            }
        }

        /// <summary>
        /// Parses a CycloneDX file and merges its components and dependencies into the accumulators.
        /// </summary>
        /// <param name="filepath">CycloneDX file path.</param>
        /// <param name="appSettings">Application settings for filtering.</param>
        /// <param name="componentsForBOM">Reference component list.</param>
        /// <param name="bom">Reference BOM to update.</param>
        /// <param name="dependencies">Reference dependency list.</param>
        private void ProcessCycloneDXFile(string filepath, CommonAppSettings appSettings, ref List<Component> componentsForBOM, ref Bom bom, ref List<Dependency> dependencies)
        {
            if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
            {
                return;
            }

            Logger.DebugFormat("ProcessCycloneDXFile():CycloneDX file detected: {0}", filepath);
            bom = ParseCycloneDXBom(filepath);

            if (bom.Components != null)
            {
                bom = RemoveExcludedComponents(appSettings, bom);
                CheckValidComponentsForProjectType(bom.Components, appSettings.ProjectType);
                AddingIdentifierType(bom.Components, "CycloneDXFile", filepath);
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += bom.Components.Count;
                componentsForBOM.AddRange(bom.Components);
                LogHandlingHelper.IdentifierInputFileComponents(filepath, bom.Components);
            }

            if (bom.Dependencies != null)
            {
                dependencies.AddRange(bom.Dependencies);
            }
        }

        /// <summary>
        /// Parses an SPDX file, validates and merges components and unsupported ones.
        /// </summary>
        /// <param name="filepath">SPDX file path.</param>
        /// <param name="appSettings">Application settings.</param>
        /// <param name="componentsForBOM">Reference component list.</param>
        /// <param name="bom">Reference BOM to update.</param>
        /// <param name="dependencies">Reference dependency list.</param>
        private void ProcessSPDXFile(string filepath, CommonAppSettings appSettings, ref List<Component> componentsForBOM, ref Bom bom, ref List<Dependency> dependencies)
        {
            BomHelper.NamingConventionOfSPDXFile(filepath, appSettings);
            Bom listUnsupportedComponents = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
            bom = _spdxBomParser.ParseSPDXBom(filepath);
            LogHandlingHelper.IdentifierInputFileComponents(filepath, bom.Components);
            bom = RemoveExcludedComponents(appSettings, bom);
            SpdxSbomHelper.CheckValidComponentsFromSpdxfile(bom, appSettings.ProjectType, ref listUnsupportedComponents);
            AddingIdentifierType(bom.Components, "SpdxFile", filepath);
            AddingIdentifierType(listUnsupportedComponents.Components, "SpdxFile", filepath);
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += bom.Components.Count;
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += listUnsupportedComponents.Components.Count;
            componentsForBOM.AddRange(bom.Components);
            dependencies.AddRange(bom.Dependencies);
            ListUnsupportedComponentsForBom.Components.AddRange(listUnsupportedComponents.Components);
            ListUnsupportedComponentsForBom.Dependencies.AddRange(listUnsupportedComponents.Dependencies);
        }
        /// <summary>
        /// Processes a package file (package-lock.json) and extracts components and dependencies.
        /// </summary>
        /// <param name="filepath">Path to the package file.</param>
        /// <param name="appSettings">Application settings.</param>
        /// <param name="componentsForBOM">Reference component list.</param>
        /// <param name="dependencies">Reference dependency list.</param>
        private static void ProcessPackageFile(string filepath, CommonAppSettings appSettings, ref List<Component> ListofComponentsFromLockFile, ref List<Dependency> ListofdependenciesFromLockFile)

        {
            Logger.Debug("ProcessPackageFile():Found as Package File");
            CommonHelper.WarnIfDependencyFileRequired();
            var components = ParsePackageLockJson(filepath, appSettings);
            var dependenciesFromPackageFiles = new List<Dependency>();
            AddingIdentifierType(components, "PackageFile", filepath);
            ListofComponentsFromLockFile.AddRange(components);
            GetDependencyDetails(components, dependenciesFromPackageFiles);
            ListofdependenciesFromLockFile.AddRange(dependenciesFromPackageFiles);
            LogHandlingHelper.IdentifierInputFileComponents(filepath, components);
        }

        /// <summary>
        /// Builds CycloneDX dependency relationships from internal manufacturer references.
        /// </summary>
        /// <param name="componentsForBOM">List of components for which to build dependencies.</param>
        /// <param name="dependencies">Dependency list to populate.</param>
        public static void GetDependencyDetails(List<Component> componentsForBOM, List<Dependency> dependencies)
        {
            Logger.Debug("GetdependencyDetails(): Starting dependency extraction process.");
            List<Dependency> dependencyList = new();

            foreach (var component in componentsForBOM)
            {
                if ((component.Manufacturer?.BomRef?.Split(",")) != null)
                {
                    Logger.DebugFormat("GetdependencyDetails():Processing component for dependency extraction: [Name: {0}, Version: {1}, PURL: {2}, Author(s): {3}]", component.Name, component.Version, component.Purl, component.Manufacturer?.BomRef);
                    List<Dependency> subDependencies = new();
                    foreach (var item in (component.Manufacturer?.BomRef?.Split(",")).Where(item => item.Contains(':')))
                    {
                        var componentDetails = item.Split(":");
                        string name;
                        string version;

                        if (componentDetails.Length >= 3 && componentDetails[2].Contains('@'))
                        {
                            var npmDetails = componentDetails[2].Split('@');
                            name = StringFormat(npmDetails[0]);
                            version = StringFormat(npmDetails[1]);
                        }
                        else
                        {
                            name = StringFormat(componentDetails[0]);
                            version = StringFormat(componentDetails[1]);
                        }
                        string purlId = $"{ApiConstant.NPMExternalID}{name}@{version}";
                        Dependency dependentList = new Dependency()
                        {
                            Ref = purlId
                        };
                        subDependencies.Add(dependentList);
                    }

                    var dependency = new Dependency()
                    {
                        Ref = component.BomRef,
                        Dependencies = subDependencies
                    };
                    Logger.DebugFormat("GetdependencyDetails():Final Dependency for Component: Ref = {0}, Sub-Dependencies = [{1}]\n", dependency.Ref, string.Join(", ", dependency.Dependencies.Select(d => d.Ref)));
                    dependencyList.Add(dependency);

                    component.Manufacturer = null;

                }
                component.Manufacturer = null;
            }
            dependencies.AddRange(dependencyList);
            Logger.Debug("GetdependencyDetails(): Completed dependency extraction process.");
        }

        /// <summary>
        /// Normalizes component identifier strings replacing known characters.
        /// </summary>
        /// <param name="componentInfo">Raw component info string.</param>
        /// <returns>Formatted string safe for PURL composition.</returns>
        private static string StringFormat(string componentInfo)
        {
            var replacements = new Dictionary<string, string> { { "\"", "" }, { "{", "" }, { "\r", "" }, { "}", "" }, { "\n", "" } };

            var formattedstring = replacements.Aggregate(componentInfo, (current, replacement) => current.Replace(replacement.Key, replacement.Value));
            return formattedstring.Trim();
        }

        /// <summary>
        /// Determines whether a JToken represents a dev dependency and increments counter when true.
        /// </summary>
        /// <param name="devValue">JToken value to test.</param>
        /// <param name="noOfDevDependent">Reference counter incremented for dev dependencies.</param>
        /// <returns>True if devValue indicates a dev dependency; otherwise false.</returns>
        private static bool IsDevDependency(JToken devValue, ref int noOfDevDependent)
        {
            if (devValue != null)
            {
                noOfDevDependent++;
            }

            return devValue != null;
        }

        /// <summary>
        /// Collects bundled:true components from a dependency map into the bundledComponents list.
        /// </summary>
        /// <param name="subdependencies">JToken representing sub-dependencies.</param>
        /// <param name="bundledComponents">Accumulator list for bundled components.</param>
        private static void GetBundledComponents(JToken subdependencies, ref List<BundledComponents> bundledComponents)
        {
            //changes for components with property "bundled:true" shouldn't be on BOMs 
            //and finally in SW360 Portal
            //checking for dependencies of each component         
            if (subdependencies != null)
            {
                foreach (JProperty sub in subdependencies.OfType<JProperty>())
                {
                    var dependentProperty = JObject.Parse(Convert.ToString(sub.Value));
                    string version = Convert.ToString(dependentProperty[Version]);

                    //check for duplicate components in the list
                    if (dependentProperty[Bundled] != null &&
                       (!bundledComponents.Any(x => x.Name == sub.Name && x.Version.Equals(version, StringComparison.InvariantCultureIgnoreCase))))
                    {
                        BundledComponents component = new() { Name = sub.Name, Version = version };
                        bundledComponents.Add(component);
                    }
                }
            }
        }

        /// <summary>
        /// Removes components that are identified as bundled from the list of components.
        /// </summary>
        /// <param name="bundledComponents">List of bundled component identifiers.</param>
        /// <param name="lstComponentForBOM">Component list to filter.</param>
        /// <returns>Filtered list with bundled components removed.</returns>
        private static List<Component> RemoveBundledComponentFromList(List<BundledComponents> bundledComponents, List<Component> lstComponentForBOM)
        {
            List<Component> components = [.. lstComponentForBOM];

            foreach (var componentsToBOM in lstComponentForBOM.Where(x => bundledComponents.Any(y => y.Name == x.Name &&
               y.Version.Equals(x.Version, StringComparison.InvariantCultureIgnoreCase))))
            {
                components.Remove(componentsToBOM);
            }
            return components;
        }

        /// <summary>
        /// Determines whether a component is internal by checking npm.name and npm.version properties in AQL results.
        /// </summary>
        /// <param name="aqlResultList">AQL result entries to search.</param>
        /// <param name="component">Component to check.</param>
        /// <param name="bomHelper">BOM helper for full-name resolution.</param>
        /// <returns>True if the component exists in the provided AQL results; otherwise false.</returns>
        private static bool IsInternalNpmComponent(
            List<AqlResult> aqlResultList, Component component, IBomHelper bomHelper)
        {
            string jfrogcomponentName = bomHelper.GetFullNameOfComponent(component);
            if (aqlResultList.Exists(x => x.Properties.Any(p => p.Key == "npm.name" && p.Value == jfrogcomponentName) && x.Properties.Any(p => p.Key == "npm.version" && p.Value == component.Version)))
            {
                Logger.DebugFormat("IsInternalNpmComponent(): Component [Name: {0}, Version: {1}] is internal,Found in JFrog repository with full name: {2}.", component.Name, component.Version, jfrogcomponentName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Locates a JFrog AQL result for an NPM component and returns repo details and path.
        /// </summary>
        /// <param name="aqlResultList">AQL results to search.</param>
        /// <param name="component">Component to locate.</param>
        /// <param name="bomHelper">BOM helper for full-name calculation.</param>
        /// <param name="jfrogRepoPath">Outputs the repo path when found.</param>
        /// <returns>Located AqlResult or a default empty result when none found.</returns>
        public static AqlResult GetJfrogArtifactoryRepoDetials(List<AqlResult> aqlResultList,
                                                                Component component,
                                                                IBomHelper bomHelper,
                                                                out string jfrogRepoPath)
        {
            Logger.DebugFormat("GetJfrogArtifactoryRepoDetials(): Starting identify JFrog repository details retrieval for component [Name: {0}, Version: {1}].", component.Name, component.Version);
            AqlResult aqlResult = new AqlResult();
            jfrogRepoPath = Dataconstant.JfrogRepoPathNotFound;
            string jfrogpackageName = bomHelper.GetFullNameOfComponent(component);
            Logger.DebugFormat("GetJfrogArtifactoryRepoDetials(): Searching for component in JFrog repository with name: {0}.", jfrogpackageName);
            var aqlResults = aqlResultList.FindAll(x => x.Properties.Any(p => p.Key == "npm.name" && p.Value == jfrogpackageName) && x.Properties.Any(p => p.Key == "npm.version" && p.Value == component.Version));

            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);

            // Forming Jfrog repo Path
            if (!repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                aqlResult = aqlResults.FirstOrDefault(x => x.Repo.Equals(repoName));
                jfrogRepoPath = GetJfrogRepoPath(aqlResult);
            }
            Logger.DebugFormat("GetJfrogArtifactoryRepoDetials(): JFrog repository path: {0}.", jfrogRepoPath);
            if (aqlResult != null)
            {
                aqlResult.Repo ??= NotFoundInRepo;
            }
            return aqlResult;
        }

        /// <summary>
        /// Formats a JFrog repo path from an AQL result entry.
        /// </summary>
        /// <param name="aqlResult">AQL result entry.</param>
        /// <returns>Formatted repo path.</returns>
        public static string GetJfrogRepoPath(AqlResult aqlResult)
        {
            if (string.IsNullOrEmpty(aqlResult.Path) || aqlResult.Path.Equals("."))
            {
                return $"{aqlResult.Repo}/{aqlResult.Name}";
            }

            return $"{aqlResult.Repo}/{aqlResult.Path}/{aqlResult.Name}";
        }


        /// <summary>
        /// Adds identification properties to components depending on the source (package, SPDX, CycloneDX).
        /// </summary>
        /// <param name="components">List of components to annotate.</param>
        /// <param name="identifiedBy">Source identifier string (e.g., "PackageFile", "SpdxFile").</param>
        /// <param name="filePath">Source file path used for SPDX filename property when applicable.</param>
        private static void AddingIdentifierType(List<Component> components, string identifiedBy, string filePath)
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
                    string fileName = Path.GetFileName(filePath);
                    if (identifiedBy == "SpdxFile")
                    {
                        SpdxSbomHelper.AddSpdxComponentProperties(fileName, component);
                    }
                    else
                    {
                        var properties = component.Properties;
                        CommonHelper.RemoveDuplicateAndAddProperty(ref properties,
                            Dataconstant.Cdx_IdentifierType,
                            Dataconstant.ManullayAdded);
                        CommonHelper.RemoveDuplicateAndAddProperty(ref properties,
                            Dataconstant.Cdx_IsDevelopment,
                            FalseString);
                        component.Properties = properties;
                    }
                }
            }
        }

        #endregion
    }
}
