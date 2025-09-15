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
        #region public methods
        public Bom ParsePackageFile(CommonAppSettings appSettings, ref Bom unSupportedBomList)
        {
            List<Component> componentsForBOM;
            Bom bom = new Bom();
            int totalUnsupportedComponentsIdentified = 0;
            ParsingInputFileForBOM(appSettings, ref bom);
            componentsForBOM = bom.Components;

            componentsForBOM = GetExcludedComponentsList(componentsForBOM);
            componentsForBOM = componentsForBOM.Distinct(new ComponentEqualityComparer()).ToList();           

            bom.Components = componentsForBOM;
            bom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(bom.Components, bom.Dependencies);
            totalUnsupportedComponentsIdentified = ListUnsupportedComponentsForBom.Components.Count;
            BomCreator.bomKpiData.ComponentsinPackageLockJsonFile += ListUnsupportedComponentsForBom.Components.Count;
            ListUnsupportedComponentsForBom.Components = ListUnsupportedComponentsForBom.Components.Distinct(new ComponentEqualityComparer()).ToList();
            BomCreator.bomKpiData.DuplicateComponents += totalUnsupportedComponentsIdentified - ListUnsupportedComponentsForBom.Components.Count;
            ListUnsupportedComponentsForBom.Dependencies = CommonHelper.RemoveInvalidDependenciesAndReferences(ListUnsupportedComponentsForBom.Components, ListUnsupportedComponentsForBom.Dependencies);
            if (bom.Components != null)
            {
                AddSiemensDirectProperty(ref bom);
            }
            AddSiemensDirectProperty(ref ListUnsupportedComponentsForBom);
            unSupportedBomList.Components = ListUnsupportedComponentsForBom.Components;
            unSupportedBomList.Dependencies = ListUnsupportedComponentsForBom.Dependencies;
            Logger.Debug($"ParsePackageFile():End");
            return bom;
        }

        public async Task<ComponentIdentification> IdentificationOfInternalComponents(ComponentIdentification componentData, CommonAppSettings appSettings,
            IJFrogService jFrogService, IBomHelper bomhelper)
        {
            List<AqlResult> aqlResultList =
                await bomhelper.GetCargoListOfComponentsFromRepo(appSettings.Cargo.Artifactory.InternalRepos, jFrogService);
            var inputIterationList = componentData.comparisonBOMData;
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(
                inputIterationList,
                component => IsInternalCargoComponent(aqlResultList, component));
            componentData.comparisonBOMData = processedComponents;
            componentData.internalComponents = internalComponents;

            return componentData;
        }


        public async Task<List<Component>> GetJfrogRepoDetailsOfAComponent(List<Component> componentsForBOM, CommonAppSettings appSettings, IJFrogService jFrogService, IBomHelper bomhelper)
        {
            string[] repoList = CommonHelper.GetRepoList(appSettings);
            List<AqlResult> aqlResultList = await bomhelper.GetCargoListOfComponentsFromRepo(repoList, jFrogService);
            Property projectType = new() { Name = Dataconstant.Cdx_ProjectType, Value = appSettings.ProjectType };
            List<Component> modifiedBOM = new List<Component>();

            foreach (var component in componentsForBOM)
            {
                var processedComponent = ProcessCargoComponent(component, aqlResultList, bomhelper, appSettings, projectType);
                modifiedBOM.Add(processedComponent);
            }
            return modifiedBOM;
        }



        #endregion

        #region private methods

        private static Component ProcessCargoComponent(Component component, List<AqlResult> aqlResultList, IBomHelper bomhelper, CommonAppSettings appSettings, Property projectType)
        {
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
            List<string> configFiles;
            List<Dependency> dependencies = new List<Dependency>();
            List<Component> componentsForBOM = new List<Component>();
            configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Cargo);
            List<string> listOfTemplateBomfilePaths = new List<string>();
            foreach (string filepath in configFiles)
            {
                if (filepath.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listOfTemplateBomfilePaths.Add(filepath);
                }
                if (filepath.Contains(FileConstant.CargoFileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    List<Component> components=new List<Component>();
                    List<Dependency> deps = new List<Dependency>();
                    Logger.Debug($"ParsingInputFileForBOM():Found metadata.json: " + filepath);
                    GetPackagesFromCargoMetadataJson(filepath, components, deps);
                    AddingIdentifierType(components, "PackageFile");
                    componentsForBOM.AddRange(components);
                    dependencies.AddRange(deps);
                }
               
                else if (filepath.EndsWith(FileConstant.SPDXFileExtension))
                {
                    Logger.Debug($"ParsingInputFileForBOM():Found as SPDXFile: " + filepath);
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
                    GetDetailsforManuallyAddedComp(bom.Components);
                    componentsForBOM.AddRange(bom.Components);
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
                var purlToDevKinds = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);                
                
                var excludeIds = (packageDetails.Workspace_members ?? Enumerable.Empty<string>())
                    .Concat(packageDetails.Workspace_default_members ?? Enumerable.Empty<string>())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                ParseCargoPackagesExcluding(packageDetails, components, idToComponent, idToPurl, excludeIds);
                AnalyzeCargoDependencyKindsExcluding(packageDetails, idToPurl, purlToDevKinds, idToComponent, dependencies, excludeIds);
                MarkCargoDevelopmentProperties(components, purlToDevKinds);
    
            }
            catch (FileNotFoundException ex)
            {
                Logger.Error("Exception in reading cargo metadata json file", ex);
                Logger.Debug($"GetPackagesFromCargoMetadataJson: File not found: {metadataJsonPath}", ex);
            }
            catch (JsonException ex)
            {
                Logger.Error("Exception in reading cargo metadata json file", ex);
                Logger.Debug($"GetPackagesFromCargoMetadataJson: JSON deserialization error in file: {metadataJsonPath}", ex);
            }
            
        }

        private static void ParseCargoPackagesExcluding(CargoPackageDetails packageDetails,List<Component> components,Dictionary<string, Component> idToComponent,Dictionary<string, string> idToPurl,List<string> excludeIds)
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

        private static void AnalyzeCargoDependencyKindsExcluding(CargoPackageDetails packageDetails,Dictionary<string, string> idToPurl,Dictionary<string, List<string>> purlToDevKinds,Dictionary<string, Component> idToComponent,List<Dependency> dependencies,List<string> excludeIds)
        {
            var resolve = packageDetails.ResolveInfo;
            if (resolve?.Nodes == null)
                return;

            foreach (var node in resolve.Nodes)
            {
                string parentId = node.Id;
                if (string.IsNullOrEmpty(parentId) || excludeIds.Contains(parentId))
                    continue;
               
                if (node.Deps != null)
                {
                    foreach (var dep in node.Deps)
                    {
                        if (dep == null || string.IsNullOrEmpty(dep.Pkg))
                            continue;
                        if (!idToPurl.TryGetValue(dep.Pkg, out var depPurl))
                            continue;
                        if (!purlToDevKinds.TryGetValue(depPurl, out var kindList))
                        {
                            kindList = new List<string>();
                            purlToDevKinds[depPurl] = kindList;
                        }

                        if (dep.DepKinds != null && dep.DepKinds.Count > 0)
                        {
                            foreach (var kind in dep.DepKinds)
                            {
                                kindList.Add(kind?.Kind);
                            }
                        }
                        else
                        {
                            kindList.Add(null);
                        }
                    }
                }

                AddCycloneDXDependencyExcluding(node, idToComponent, parentId, dependencies, excludeIds);
            }
        }

        private static void AddCycloneDXDependencyExcluding(CargoPackageDetails.Node node,Dictionary<string, Component> idToComponent,string parentId,List<Dependency> dependencies,List<string> excludeIds)
        {
            if (node.Deps == null || node.Deps.Count == 0)
            {
                return;
            }

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
                return true;
            }

            return false;
        }
        private static List<Component> GetExcludedComponentsList(List<Component> componentsForBOM)
        {
            List<Component> components = new List<Component>();
            foreach (Component componentsInfo in componentsForBOM)
            {
                if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version) && !string.IsNullOrEmpty(componentsInfo.Purl) && componentsInfo.Purl.Contains(Dataconstant.PurlCheck()["CARGO"]))
                {
                    components.Add(componentsInfo);
                    Logger.Debug($"GetExcludedComponentsList():ValidComponent For CARGO : Component Details : {componentsInfo.Name} @ {componentsInfo.Version} @ {componentsInfo.Purl}");
                }
                else
                {
                    BomCreator.bomKpiData.ComponentsExcluded++;
                    Logger.Debug($"GetExcludedComponentsList():InvalidComponent For CARGO : Component Details : {componentsInfo.Name} @ {componentsInfo.Version} @ {componentsInfo.Purl}");
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
                // Initialize properties list if null, otherwise keep existing properties
                component.Properties ??= new List<Property>();
                var properties = component.Properties;
                // Use helper method to safely add properties without duplicates
                CommonHelper.RemoveDuplicateAndAddProperty(ref properties,
                    Dataconstant.Cdx_IsDevelopment,
                    "false");
                CommonHelper.RemoveDuplicateAndAddProperty(ref properties,
                    Dataconstant.Cdx_IdentifierType,
                    Dataconstant.ManullayAdded);
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
