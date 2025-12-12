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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static CycloneDX.Models.ExternalReference;
using Dependency = CycloneDX.Models.Dependency;
using File = System.IO.File;

namespace LCT.PackageIdentifier
{
    public class CargoProcessor(ICycloneDXBomParser cycloneDXBomParser, ISpdxBomParser spdxBomParser) : CycloneDXBomParser, IParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        private static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
        private const string NotFoundInRepo = "Not Found in JFrogRepo";
        private readonly IEnvironmentHelper environmentHelper = new EnvironmentHelper();
        private List<Component> listOfInternalComponents = new List<Component>();
        #region public methods
        public Bom ParsePackageFile(CommonAppSettings appSettings, ref Bom unSupportedBomList)
        {
            Logger.Debug($"ParsePackageFile():Started Parseing input File for Cargo components.");
            List<Component> componentsForBOM;
            Bom bom = new Bom();
            ParsingInputFileForBOM(appSettings, ref bom);
            componentsForBOM = bom.Components;

            componentsForBOM = BomHelper.GetExcludedComponentsList(componentsForBOM, Dataconstant.PurlCheck()["CARGO"], appSettings?.ProjectType);
            componentsForBOM = componentsForBOM.Distinct(new ComponentEqualityComparer()).ToList();

            bom.Components = componentsForBOM;
            bom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(bom.Components, bom.Dependencies);
            int totalUnsupportedComponentsIdentified = ListUnsupportedComponentsForBom.Components.Count;
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += ListUnsupportedComponentsForBom.Components.Count;
            ListUnsupportedComponentsForBom.Components = ListUnsupportedComponentsForBom.Components.Distinct(new ComponentEqualityComparer()).ToList();
            BomCreator.bomKpiData.DuplicateComponents += totalUnsupportedComponentsIdentified - ListUnsupportedComponentsForBom.Components.Count;
            ListUnsupportedComponentsForBom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(ListUnsupportedComponentsForBom.Components, ListUnsupportedComponentsForBom.Dependencies);
            AddSiemensDirectProperty(ref ListUnsupportedComponentsForBom);
            unSupportedBomList.Components = ListUnsupportedComponentsForBom.Components;
            unSupportedBomList.Dependencies = ListUnsupportedComponentsForBom.Dependencies;
            Logger.Debug($"ParsePackageFile():Completed Parseing input File for Cargo components.\n");
            return bom;
        }

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData, CommonAppSettings appSettings,
            IJFrogService jFrogService, IBomHelper bomhelper)
        {
            Logger.Debug("IdentificationOfInternalComponents(): Starting identification of internal components.");
            List<AqlResult> aqlResultList =
                await bomhelper.GetCargoListOfComponentsFromRepo(appSettings.Cargo.Artifactory.InternalRepos, jFrogService);
            var inputIterationList = componentData.comparisonBOMData;
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(
                inputIterationList,
                component => IsInternalCargoComponent(aqlResultList, component));
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
            string[] repoList = CommonHelper.GetRepoList(appSettings);
            List<AqlResult> aqlResultList = await bomhelper.GetCargoListOfComponentsFromRepo(repoList, jFrogService);
            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                var processedComponent = ProcessCargoComponent(component, aqlResultList, bomhelper, appSettings, projectType);
                modifiedBOM.Add(processedComponent);
            }
            LogHandlingHelper.IdentifierComponentsData(componentsForBOM, listOfInternalComponents);
            Logger.Debug("GetJfrogRepoDetailsOfAComponent():Completed retrieving JFrog repository details for components.\n");
            return modifiedBOM;
        }



        #endregion

        #region private methods

        private static Component ProcessCargoComponent(Component component, List<AqlResult> aqlResultList, IBomHelper bomhelper, CommonAppSettings appSettings, Property projectType)
        {
            Logger.Debug($"GetArtifactoryRepoName(): Starting identify JFrog repository details retrieval for component [Name: {component.Name}, Version: {component.Version}].");
            string repoName = GetArtifactoryRepoName(aqlResultList, component, bomhelper, out string jfrogPackageNameWhlExten, out string jfrogRepoPath);

            var hashes = aqlResultList.FirstOrDefault(x => x.Properties.Any(p => p.Key == "crate.name" && p.Value == component.Name) && x.Properties.Any(p => p.Key == "crate.version" && p.Value == component.Version));

            Property artifactoryrepo = new() { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = repoName };
            Property fileNameProperty = new() { Name = Dataconstant.Cdx_Siemensfilename, Value = jfrogPackageNameWhlExten };
            Property jfrogRepoPathProperty = new() { Name = Dataconstant.Cdx_JfrogRepoPath, Value = jfrogRepoPath };
            Component componentVal = component;

            UpdateCargoKpiDataBasedOnRepo(artifactoryrepo.Value, appSettings);

            // Use common helper to set component properties and hashes
            CommonHelper.SetComponentPropertiesAndHashes(componentVal, artifactoryrepo, projectType, fileNameProperty, jfrogRepoPathProperty, hashes);

            return componentVal;
        }

        private static void UpdateCargoKpiDataBasedOnRepo(string repoValue, CommonAppSettings appSettings)
        {
            if (repoValue == appSettings.Cargo.DevDepRepo)
            {
                BomCreator.bomKpiData.DevdependencyComponents++;
            }

            if (appSettings.Cargo.Artifactory.ThirdPartyRepos != null)
            {
                foreach (var thirdPartyRepo in appSettings.Cargo.Artifactory.ThirdPartyRepos)
                {
                    if (repoValue == thirdPartyRepo.Name)
                    {
                        BomCreator.bomKpiData.ThirdPartyRepoComponents++;
                        break;
                    }
                }
            }

            if (repoValue == appSettings.Cargo.ReleaseRepo)
            {
                BomCreator.bomKpiData.ReleaseRepoComponents++;
            }

            if (repoValue == Dataconstant.NotFoundInJFrog || repoValue == "")
            {
                BomCreator.bomKpiData.UnofficialComponents++;
            }
        }

        private static string GetArtifactoryRepoName(List<AqlResult> aqlResultList,
                                                     Component component,
                                                     IBomHelper bomHelper,
                                                     out string jfrogPackageName,
                                                     out string jfrogRepoPath)
        {
            jfrogPackageName = Dataconstant.PackageNameNotFoundInJfrog;
            jfrogRepoPath = Dataconstant.JfrogRepoPathNotFound;
            string jfrogPackageNameWhlExten = GetJfrogNameOfCargoComponent(
                component.Name, component.Version, aqlResultList);

            var aqlResults = aqlResultList.FindAll(x => x.Name.Equals(
                jfrogPackageNameWhlExten, StringComparison.OrdinalIgnoreCase));
            jfrogPackageName = jfrogPackageNameWhlExten;

            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);

            if (repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                string fullName = bomHelper.GetFullNameOfComponent(component);
                string fullNameVersion = GetJfrogNameOfCargoComponent(fullName, component.Version, aqlResultList);
                if (!fullNameVersion.Equals(jfrogPackageNameWhlExten, StringComparison.OrdinalIgnoreCase))
                {
                    var aqllist = aqlResultList.FindAll(x => x.Name.Contains(fullNameVersion, StringComparison.OrdinalIgnoreCase)
                    && x.Name.EndsWith(ApiConstant.CargoExtension));
                    jfrogPackageName = fullNameVersion;
                    repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqllist);
                }
            }

            // Forming Jfrog repo Path
            if (!repoName.Equals(NotFoundInRepo, StringComparison.OrdinalIgnoreCase))
            {
                var aqlResult = aqlResults.FirstOrDefault(x => x.Repo.Equals(repoName));
                jfrogRepoPath = GetJfrogRepoPath(aqlResult);
            }

            if (string.IsNullOrEmpty(jfrogPackageName))
            {
                jfrogPackageName = Dataconstant.PackageNameNotFoundInJfrog;
            }

            return repoName;
        }
        private static string GetJfrogNameOfCargoComponent(string name, string version, List<AqlResult> aqlResultList)
        {
            string nameVerison = string.Empty;
            nameVerison = aqlResultList.FirstOrDefault(x => x.Properties.Any(p => p.Key == "crate.name" && p.Value == name) && x.Properties.Any(p => p.Key == "crate.version" && p.Value == version))?.Name ?? string.Empty;

            if (string.IsNullOrEmpty(nameVerison))
            {
                nameVerison = Dataconstant.PackageNameNotFoundInJfrog;
            }
            return nameVerison;
        }
        private static string GetJfrogRepoPath(AqlResult aqlResult)
        {
            if (string.IsNullOrEmpty(aqlResult.Path) || aqlResult.Path.Equals("."))
            {
                return $"{aqlResult.Repo}/{aqlResult.Name}";
            }

            return $"{aqlResult.Repo}/{aqlResult.Path}/{aqlResult.Name}";
        }

        private void ParsingInputFileForBOM(CommonAppSettings appSettings, ref Bom bom)
        {
            var configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Cargo,environmentHelper);
            var componentsForBOM = new List<Component>();
            var dependencies = new List<Dependency>();
            var templateBomFilePaths = new List<string>();

            foreach (var filepath in configFiles)
            {
                InputfilesProcess(filepath, appSettings, ref bom, componentsForBOM, dependencies, templateBomFilePaths);
            }

            BomFileDataProcess(ref bom, componentsForBOM, dependencies, templateBomFilePaths, appSettings);
        }

        private void InputfilesProcess(
            string filepath,
            CommonAppSettings appSettings,
            ref Bom bom,
            List<Component> componentsForBOM,
            List<Dependency> dependencies,
            List<string> templateBomFilePaths)
        {
            if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
            {
                templateBomFilePaths.Add(filepath);
            }
            else if (filepath.EndsWith(FileConstant.CargoFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                ParseCargoFile(filepath, componentsForBOM, dependencies);
            }
            else if (filepath.EndsWith(FileConstant.SPDXFileExtension))
            {
                ParseSpdxFile(filepath, appSettings, ref bom, componentsForBOM, dependencies);
            }
            else if (filepath.EndsWith(FileConstant.CycloneDXFileExtension) &&
                     !filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
            {
                ParseCycloneDxFile(filepath, appSettings, ref bom, componentsForBOM);
            }
        }

        private static void ParseCargoFile(string filepath, List<Component> componentsForBOM, List<Dependency> dependencies)
        {
            Logger.Debug($"ParsingInputFileForBOM():Found metadata.json: {filepath}");
            var components = new List<Component>();
            var deps = new List<Dependency>();
            GetPackagesFromCargoMetadataJson(filepath, components, deps);
            AddingIdentifierType(components);
            LogHandlingHelper.IdentifierInputFileComponents(filepath, components);
            componentsForBOM.AddRange(components);
            dependencies.AddRange(deps);
        }

        private void ParseSpdxFile(
            string filepath,
            CommonAppSettings appSettings,
            ref Bom bom,
            List<Component> componentsForBOM,
            List<Dependency> dependencies)
        {
            Logger.Debug($"ParsingInputFileForBOM():Spdx file detected: {filepath}");
            BomHelper.NamingConventionOfSPDXFile(filepath, appSettings);
            var listUnsupportedComponents = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
            bom = _spdxBomParser.ParseSPDXBom(filepath);
            SpdxSbomHelper.CheckValidComponentsFromSpdxfile(bom, appSettings.ProjectType, ref listUnsupportedComponents);
            SpdxSbomHelper.AddSpdxSBomFileNameProperty(ref bom, filepath);
            LogHandlingHelper.IdentifierInputFileComponents(filepath, bom.Components);
            componentsForBOM.AddRange(bom.Components);
            dependencies.AddRange(bom.Dependencies);
            SpdxSbomHelper.AddSpdxPropertysForUnsupportedComponents(listUnsupportedComponents.Components, filepath);
            ListUnsupportedComponentsForBom.Components.AddRange(listUnsupportedComponents.Components);
            ListUnsupportedComponentsForBom.Dependencies.AddRange(listUnsupportedComponents.Dependencies);
        }

        private void ParseCycloneDxFile(
            string filepath,
            CommonAppSettings appSettings,
            ref Bom bom,
            List<Component> componentsForBOM)
        {
            Logger.Debug($"ParsingInputFileForBOM():CycloneDX file detected: {filepath}");
            bom = _cycloneDXBomParser.ParseCycloneDXBom(filepath);
            CheckValidComponentsForProjectType(bom.Components, appSettings.ProjectType);
            BomHelper.GetDetailsforManuallyAddedComp(bom.Components);
            LogHandlingHelper.IdentifierInputFileComponents(filepath, bom.Components);
            if (bom.Components != null)
            {
                AddSiemensDirectProperty(ref bom);
            }
            componentsForBOM.AddRange(bom.Components);
        }

        private void BomFileDataProcess(
            ref Bom bom,
            List<Component> componentsForBOM,
            List<Dependency> dependencies,
            List<string> templateBomFilePaths,
            CommonAppSettings appSettings)
        {
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

            string templateFilePath = SbomTemplate.GetFilePathForTemplate(templateBomFilePaths);
            SbomTemplate.ProcessTemplateFile(templateFilePath, _cycloneDXBomParser, bom.Components, appSettings.ProjectType);

            bom = BomHelper.RemoveExcludedComponents(appSettings, bom);
            bom.Dependencies = bom.Dependencies?.GroupBy(x => new { x.Ref }).Select(y => y.First()).ToList();
        }

        private static void GetPackagesFromCargoMetadataJson(string metadataJsonPath, List<Component> components, List<Dependency> dependencies)
        {
            try
            {
                var json = File.ReadAllText(metadataJsonPath);
                CargoPackageDetails packageDetails = JsonConvert.DeserializeObject<CargoPackageDetails>(json);

                if (packageDetails == null)
                {
                    Logger.Debug($"GetPackagesFromCargoMetadataJson: Deserialized packageDetails is null for file: {metadataJsonPath}");
                    return;
                }

                var idToComponent = new Dictionary<string, Component>();
                var idToPurl = new Dictionary<string, string>();
                var purlToDevDependencyKinds = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                var excludeIds = (packageDetails.Workspace_members ?? Enumerable.Empty<string>())
                    .Concat(packageDetails.Workspace_default_members ?? Enumerable.Empty<string>())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                ParseCargoPackagesExcluding(packageDetails, components, idToComponent, idToPurl, excludeIds);
                AnalyzeCargoDependencyKindsExcluding(packageDetails, idToPurl, purlToDevDependencyKinds, idToComponent, dependencies, excludeIds);
                MarkCargoDevelopmentProperties(components, purlToDevDependencyKinds);
                AddDirectDependencyProperty(packageDetails, components, idToPurl);

            }
            catch (FileNotFoundException ex)
            {
                Logger.Error("Exception in reading cargo metadata json file", ex);
                LogHandlingHelper.ExceptionErrorHandling("Error occurred while parsing the package lock JSON file.", "GetPackagesFromCargoMetadataJson()", ex, $"File Path: {metadataJsonPath}");
            }
            catch (JsonException ex)
            {
                Logger.Error("Exception in reading cargo metadata json file", ex);
                LogHandlingHelper.ExceptionErrorHandling("JSON deserialization error in file", "GetPackagesFromCargoMetadataJson()", ex, $"File Path: {metadataJsonPath}");                
            }

        }
        public static void AddDirectDependencyProperty(CargoPackageDetails packageDetails, List<Component> components, Dictionary<string, string> idToPurl)
        {
            var directDependencyPurls = new List<string>();
            if (packageDetails.ResolveInfo?.Root != null)
            {
                var rootNode = packageDetails.ResolveInfo.Nodes?.FirstOrDefault(n => n.Id == packageDetails.ResolveInfo.Root);
                if (rootNode?.Dependencies != null)
                {
                    foreach (var depId in rootNode.Dependencies)
                    {
                        if (idToPurl.TryGetValue(depId, out var depPurl))
                        {
                            directDependencyPurls.Add(depPurl);
                        }
                    }
                }
            }
            directDependencyPurls = [.. directDependencyPurls.Distinct()];
            foreach (var component in components)
            {
                component.Properties ??= new List<Property>();
                bool isDirect = directDependencyPurls.Contains(component.Purl);
                var properties = component.Properties;
                // Add or update the property
                CommonHelper.RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_SiemensDirect, isDirect ? "true" : "false");
                component.Properties = properties;
            }
        }
        private static void ParseCargoPackagesExcluding(CargoPackageDetails packageDetails, List<Component> components, Dictionary<string, Component> idToComponent, Dictionary<string, string> idToPurl, List<string> excludeIds)
        {
            if (packageDetails.Packages == null)
                return;

            foreach (var pkg in packageDetails.Packages)
            {
                if (excludeIds.Contains(pkg.Id))
                    continue;

                string name = pkg.Name;
                string version = pkg.Version;
                string purl = Dataconstant.PurlCheck()["CARGO"] + "/" + name + "@" + version;
                string id = pkg.Id;

                var component = CommonHelper.CreateComponentWithProperties(name, version, purl);
                AddSourceUrlExternalReference(component, pkg);
                components.Add(component);

                if (!string.IsNullOrEmpty(id))
                {
                    idToComponent[id] = component;
                    idToPurl[id] = purl;
                }
            }
        }

        private static void AnalyzeCargoDependencyKindsExcluding(
    CargoPackageDetails packageDetails,
    Dictionary<string, string> idToPurl,
    Dictionary<string, List<string>> purlToDevDependencyKinds,
    Dictionary<string, Component> idToComponent,
    List<Dependency> dependencies,
    List<string> excludeIds)
        {
            var resolve = packageDetails.ResolveInfo;
            if (resolve?.Nodes == null)
                return;

            foreach (var node in resolve.Nodes)
            {

                ProcessNodeDeps(node, idToPurl, purlToDevDependencyKinds);

                AddCycloneDXDependencyExcluding(node, idToComponent, node.Id, dependencies, excludeIds);
            }
        }


        private static void ProcessNodeDeps(
            CargoPackageDetails.Node node,
            Dictionary<string, string> idToPurl,
            Dictionary<string, List<string>> purlToDevDependencyKinds)
        {
            if (node.Deps == null)
                return;

            foreach (var dep in node.Deps)
            {
                if (!IsValidDep(dep, idToPurl, out var depPurl))
                    continue;

                var dependencyKindList = GetOrCreateKindList(depPurl, purlToDevDependencyKinds);

                AddDepKinds(dep, dependencyKindList);
            }
        }

        private static bool IsValidDep(CargoPackageDetails.Dep dep, Dictionary<string, string> idToPurl, out string depPurl)
        {
            depPurl = null;
            if (dep == null || string.IsNullOrEmpty(dep.Pkg))
                return false;
            return idToPurl.TryGetValue(dep.Pkg, out depPurl);
        }

        private static List<string> GetOrCreateKindList(string depPurl, Dictionary<string, List<string>> purlToDevDependencyKinds)
        {
            if (!purlToDevDependencyKinds.TryGetValue(depPurl, out var dependencyKindList))
            {
                dependencyKindList = new List<string>();
                purlToDevDependencyKinds[depPurl] = dependencyKindList;
            }
            return dependencyKindList;
        }

        private static void AddDepKinds(CargoPackageDetails.Dep dep, List<string> dependencyKindList)
        {
            if (dep.DepKinds != null && dep.DepKinds.Count > 0)
            {
                foreach (var kind in dep.DepKinds)
                {
                    dependencyKindList.Add(kind?.Kind);
                }
            }
            else
            {
                dependencyKindList.Add(null);
            }
        }

        private static void AddCycloneDXDependencyExcluding(CargoPackageDetails.Node node, Dictionary<string, Component> idToComponent, string parentId, List<Dependency> dependencies, List<string> excludeIds)
        {

            var depIds = node.Dependencies ?? new List<string>();
            var subDeps = depIds
                .Where(depId => idToComponent.ContainsKey(depId) && !excludeIds.Contains(depId))
                .Select(depId => new Dependency { Ref = idToComponent[depId].Purl })
                .ToList();

            if (idToComponent.TryGetValue(parentId, out var parentComponent))
            {
                dependencies.Add(new Dependency
                {
                    Ref = parentComponent.Purl,
                    Dependencies = subDeps
                });
            }
        }

        private static void AddSourceUrlExternalReference(Component component, CargoPackageDetails.Package pkg)
        {
            string sourceUrl = pkg.Repository;
            if (!string.IsNullOrWhiteSpace(sourceUrl))
            {
                component.ExternalReferences ??= new List<ExternalReference>();
                component.ExternalReferences.Add(new ExternalReference
                {
                    Type = ExternalReferenceType.Distribution,
                    Url = sourceUrl
                });
            }
        }

        private static void MarkCargoDevelopmentProperties(List<Component> components, Dictionary<string, List<string>> purlToDevKinds)
        {
            foreach (var component in components)
            {
                var purl = component.Purl;
                bool isDevOrBuild = false;
                if (purlToDevKinds.TryGetValue(purl, out var kindSet))
                {
                    var kinds = new List<string>(kindSet.Select(k => k?.ToLowerInvariant() ?? "null"));

                    bool hasNull = kinds.Contains("null");
                    bool hasDev = kinds.Contains("dev");
                    bool hasBuild = kinds.Contains("build");

                    if (hasNull)
                    {
                        isDevOrBuild = false;
                    }
                    else if (hasDev || hasBuild)
                    {
                        isDevOrBuild = true;
                    }
                }
                var properties = component.Properties;
                CommonHelper.RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_IsDevelopment, isDevOrBuild ? "true" : "false");
                component.Properties = properties;
                if (isDevOrBuild)
                {
                    BomCreator.bomKpiData.DevDependentComponents++;
                }
            }
        }
        private static bool IsInternalCargoComponent(List<AqlResult> aqlResultList, Component component)
        {
            if (aqlResultList.Exists(x => x.Properties.Any(p => p.Key == "crate.name" && p.Value == component.Name) && x.Properties.Any(p => p.Key == "crate.version" && p.Value == component.Version)))
            {
                Logger.Debug($"IsInternalCargoComponent(): Component [Name: {component.Name}, Version: {component.Version}] is internal.");
                return true;
            }

            return false;
        }

        private static void AddingIdentifierType(List<Component> components)
        {
            foreach (var component in components)
            {
                component.Properties ??= new List<Property>();

                var properties = component.Properties;
                CommonHelper.RemoveDuplicateAndAddProperty(ref properties,
                    Dataconstant.Cdx_IdentifierType,
                    Dataconstant.Discovered);
                component.Properties = properties;
            }
        }

        public static void AddSiemensDirectProperty(ref Bom bom)
        {
            List<string> cargoDirectDependencies = new List<string>();
            cargoDirectDependencies.AddRange(bom.Dependencies?.Select(x => x.Ref).ToList() ?? new List<string>());
            var bomComponentsList = bom.Components;
            foreach (var component in bomComponentsList)
            {
                Property siemensDirect = new() { Name = Dataconstant.Cdx_SiemensDirect, Value = "false" };
                if (cargoDirectDependencies.Exists(x => x.Contains(component.Name) && x.Contains(component.Version)))
                {
                    siemensDirect.Value = "true";
                }

                component.Properties ??= new List<Property>();
                bool isPropExists = component.Properties.Exists(
                    x => x.Name.Equals(Dataconstant.Cdx_SiemensDirect));

                if (!isPropExists) { component.Properties.Add(siemensDirect); }
            }

            bom.Components = bomComponentsList;
        }
        #endregion
    }
}
