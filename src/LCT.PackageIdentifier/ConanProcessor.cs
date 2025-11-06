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
        #region fields
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        private static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };

        #endregion

        #region public methods
        public Bom ParsePackageFile(CommonAppSettings appSettings, ref Bom unSupportedBomList)
        {
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
            Logger.Debug($"ParsePackageFile():End");
            return bom;
        }

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData, CommonAppSettings appSettings,
            IJFrogService jFrogService, IBomHelper bomhelper)
        {
            List<AqlResult> aqlResultList =
                await bomhelper.GetListOfComponentsFromRepo(appSettings.Conan.Artifactory.InternalRepos, jFrogService);
            var inputIterationList = componentData.comparisonBOMData;
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(
                inputIterationList,
                component => IsInternalConanComponent(aqlResultList, component));
            componentData.comparisonBOMData = processedComponents;
            componentData.internalComponents = internalComponents;

            return componentData;
        }


        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
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

            return modifiedBOM;
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
            List<string> configFiles;
            List<Dependency> dependencies = new List<Dependency>();
            List<Component> componentsForBOM = new List<Component>();
            configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Conan);
            List<string> listOfTemplateBomfilePaths = new List<string>();

            foreach (string filepath in configFiles)
            {
                if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listOfTemplateBomfilePaths.Add(filepath);
                }
                if (filepath.ToLower().EndsWith(FileConstant.ConanFileExtension))
                {
                    Logger.Debug($"ParsingInputFileForBOM():FileName: " + filepath);
                    var components = ParseDepJson(filepath, ref dependencies);
                    AddingIdentifierType(components, "PackageFile");
                    componentsForBOM.AddRange(components);
                }
                else if (filepath.EndsWith(FileConstant.SPDXFileExtension))
                {
                    BomHelper.NamingConventionOfSPDXFile(filepath, appSettings);
                    Bom listUnsupportedComponents = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
                    bom = _spdxBomParser.ParseSPDXBom(filepath);
                    SpdxSbomHelper.CheckValidComponentsFromSpdxfile(bom, appSettings.ProjectType, ref listUnsupportedComponents);
                    SpdxSbomHelper.AddSpdxSBomFileNameProperty(ref bom, filepath);
                    componentsForBOM.AddRange(bom.Components);
                    dependencies.AddRange(bom.Dependencies);
                    SpdxSbomHelper.AddSpdxPropertysForUnsupportedComponents(listUnsupportedComponents.Components, filepath);
                    ListUnsupportedComponentsForBom.Components.AddRange(listUnsupportedComponents.Components);
                    ListUnsupportedComponentsForBom.Dependencies.AddRange(listUnsupportedComponents.Dependencies);
                }
                else if (filepath.EndsWith(FileConstant.CycloneDXFileExtension)
                    && !filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    Logger.Debug($"ParsingInputFileForBOM():Found as CycloneDXFile");
                    bom = _cycloneDXBomParser.ParseCycloneDXBom(filepath);
                    CheckValidComponentsForProjectType(bom.Components, appSettings.ProjectType);
                    BomHelper.GetDetailsforManuallyAddedComp(bom.Components);
                    componentsForBOM.AddRange(bom.Components);
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
        }

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
                    Logger.Warn($"No nodes found in dep.json file: {filepath}");
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
                Logger.Error($"ParseDepJson(): Failed to parse JSON", ex);
            }
            catch (IOException ex)
            {
                Environment.ExitCode = -1;
                Logger.Error($"ParseDepJson(): IO Error", ex);
            }
            catch (SecurityException ex)
            {
                Environment.ExitCode = -1;
                Logger.Error($"ParseDepJson(): Security Error", ex);
            }

            return StartingComponentForBOM;
        }

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

        private static List<Component> GetExcludedComponentsList(List<Component> componentsForBOM)
        {
            List<Component> components = new List<Component>();
            foreach (Component componentsInfo in componentsForBOM)
            {
                if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.Purl) && componentsInfo.Purl.Contains(Dataconstant.PurlCheck()["CONAN"]))
                {
                    components.Add(componentsInfo);
                    Logger.Debug($"GetExcludedComponentsList():ValidComponent For CONAN : Component Details : {componentsInfo.Name} @ {componentsInfo.Version} @ {componentsInfo.Purl}");
                }
                else
                {
                    BomCreator.bomKpiData.ComponentsExcluded++;
                    Logger.Debug($"GetExcludedComponentsList():InvalidComponent For CONAN : Component Details : {componentsInfo.Name} @ {componentsInfo.Version} @ {componentsInfo.Purl}");
                }
            }
            return components;
        }

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
                component.Properties ??= new List<Property>();
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

        private static bool IsInternalConanComponent(List<AqlResult> aqlResultList, Component component)
        {
            string jfrogcomponentPath = $"{component.Name}/{component.Version}";
            if (aqlResultList.Exists(
                x => x.Path.Contains(jfrogcomponentPath, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        public static string GetArtifactoryRepoName(List<AqlResult> aqlResultList, Component component, out string jfrogRepoPath)
        {
            string jfrogcomponentPath = $"{component.Name}/{component.Version}";
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
        #endregion
    }
}