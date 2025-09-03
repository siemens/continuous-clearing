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
using System.Text;
using System.Threading.Tasks;
using Tommy;
using File = System.IO.File;

namespace LCT.PackageIdentifier
{
    public class CargoProcessor(ICycloneDXBomParser cycloneDXBomParser, ISpdxBomParser spdxBomParser) : CycloneDXBomParser, IParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        private static readonly char[] SplitChars = { '/', '@' };
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
            // get the  component list from Jfrog for given repo + internal repo
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

            if (string.IsNullOrEmpty(nameVerison)) { nameVerison = Dataconstant.PackageNameNotFoundInJfrog; }
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
                if (filepath.ToLower().EndsWith("cargo.lock"))
                {
                    Logger.Debug($"ParsingInputFileForBOM():FileName: " + filepath);
                    var components = GetPackagesFromCargoLockFile(filepath, ref dependencies);
                    AddingIdentifierType(components, "PackageFile");
                    componentsForBOM.AddRange(components);
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

        private static List<Component> GetPackagesFromCargoLockFile(string filePath, ref List<Dependency> dependencies)
        {
            Logger.Debug($"[GetPackagesFromCargoLockFile] Start processing Cargo.lock: {filePath}");

            // 1. Identify all packages from Cargo.lock
            List<Component> cargoPackages = GetAllPackagesFromCargoLock(filePath, out var keyValuePair, out var packageVersionLookup);

            // 2. Build the dependency list
            GetCargoRefDetailsFromDependencyText(keyValuePair, dependencies, cargoPackages, packageVersionLookup);

            // 3. Identify dev dependencies and add property
            string cargoTomlPath = Path.Combine(Path.GetDirectoryName(filePath)!, "Cargo.toml");
            var devDependencies = GetDevDependenciesFromToml(cargoTomlPath);
            MarkDevDependencies(cargoPackages, devDependencies);

            Logger.Debug("[GetPackagesFromCargoLockFile] Finished processing Cargo.lock.");
            return cargoPackages;
        }

        private static List<Component> GetAllPackagesFromCargoLock(
            string filePath,
            out List<KeyValuePair<string, TomlNode>> keyValuePair,
            out Dictionary<string, string> packageVersionLookup)
        {
            Logger.Debug($"[GetAllPackagesFromCargoLock()] Parsing Cargo.lock file: {filePath}");
            List<Component> cargoPackages = new();
            keyValuePair = new List<KeyValuePair<string, TomlNode>>();
            packageVersionLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            FileParser fileParser = new();
            TomlTable tomlTable = fileParser.ParseTomlFile(filePath);
            if (!tomlTable.Keys.Contains("package"))
            {
                Logger.Debug($"No [package] section found in Cargo.lock: {filePath}. No packages will be processed.");
                return cargoPackages;
            }
            int packageCount = 0;
            foreach (TomlNode node in tomlTable["package"])
            {
                string name = node["name"].ToString();
                string version = node["version"].ToString();
                string purl = Dataconstant.PurlCheck()["CARGO"] + "/" + name + "@" + version;

                if (!packageVersionLookup.ContainsKey(name))
                    packageVersionLookup[name] = version;

                Component cargoComponent = CommonHelper.CreateComponentWithProperties(
                    name,
                    version,
                    purl
                );

                cargoPackages.Add(cargoComponent);
                keyValuePair.Add(new KeyValuePair<string, TomlNode>(purl, node));
                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                packageCount++;
                Logger.Debug($"[GetAllPackagesFromCargoLock()] Added package: {name}@{version} (PURL: {purl})");
            }

            Logger.Debug($"[GetAllPackagesFromCargoLock()] Total packages parsed from Cargo.lock: {packageCount}");
            return cargoPackages;
        }

        private static void MarkDevDependencies(List<Component> cargoPackages, List<string> devDependencies)
        {
            foreach (var component in cargoPackages)
            {
                if (devDependencies.Contains(component.Name))
                {
                    component.Properties ??= new List<Property>();
                    component.Properties.Add(new Property
                    {
                        Name = Dataconstant.Cdx_IsDevelopment,
                        Value = "true"
                    });
                    BomCreator.bomKpiData.DevDependentComponents++;
                }
                else
                {
                    component.Properties ??= new List<Property>();
                    component.Properties.Add(new Property
                    {
                        Name = Dataconstant.Cdx_IsDevelopment,
                        Value = "false"
                    });
                }
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
        private static List<string> GetDevDependenciesFromToml(string tomlFilePath)
        {
            var devDeps = new List<string>();
            if (!File.Exists(tomlFilePath))
            {
                Logger.Warn($"Cargo.toml file not found at path: {tomlFilePath}. Dev dependencies will not be identified.");
                return devDeps;
            }

            using (var reader = new StreamReader(tomlFilePath))
            {
                TomlTable tomlTable = TOML.Parse(reader);
                if (tomlTable.HasKey("dev-dependencies"))
                {
                    var devDepsTable = tomlTable["dev-dependencies"] as TomlTable;
                    if (devDepsTable != null)
                    {
                        foreach (var key in devDepsTable.Keys)
                        {
                            devDeps.Add(key);
                        }
                    }
                }
            }
            return devDeps;
        }
        private static void GetCargoRefDetailsFromDependencyText(
            List<KeyValuePair<string, TomlNode>> keyValues,
            List<Dependency> dependencies,
            List<Component> cargoPackages,
            Dictionary<string, string> packageVersionLookup)
        {
            foreach (var node in keyValues)
            {
                var dep = node.Value["dependencies"];
                List<Dependency> subDependencies = new();
                if (dep != null && dep.ChildrenCount > 0)
                {
                    foreach (var dependency in dep)
                    {
                        // Each dependency is a string like: "name version (source)" or just "name"
                        string depStr = dependency.ToString();
                        var parts = depStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        string depName = parts[0];
                        string depVersion = parts.Length > 1 ? parts[1] : null;

                        // If version is not present, try to get it from the lookup
                        if (string.IsNullOrEmpty(depVersion) && packageVersionLookup.TryGetValue(depName, out var foundVersion))
                        {
                            depVersion = foundVersion;
                        }
                        else if (string.IsNullOrEmpty(depVersion))
                        {
                            continue;
                        }

                        string depPurl = Dataconstant.PurlCheck()["CARGO"] + "/" + depName + "@" + depVersion;

                        subDependencies.Add(new Dependency
                        {
                            Ref = depPurl
                        });
                    }
                }

                dependencies.Add(new Dependency
                {
                    Ref = node.Key,
                    Dependencies = subDependencies
                });
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
        

        public static string GetArtifactoryRepoName(List<AqlResult> aqlResultList, Component component, out string jfrogRepoPath)
        {
            string jfrogcomponentPath = $"{component.Name}/{component.Version}";
            jfrogRepoPath = Dataconstant.JfrogRepoPathNotFound;
            var cargoPackagePath = aqlResultList.FirstOrDefault(x => x.Path.Contains(jfrogcomponentPath) && x.Name.Contains("package.tgz"));
            if (cargoPackagePath != null)
            {
                jfrogRepoPath = $"{cargoPackagePath.Repo}/{cargoPackagePath.Path}/{cargoPackagePath.Name};";
            }
            var aqllist = aqlResultList.FindAll(x => x.Properties.Any(p => p.Key == "crate.name" && p.Value == component.Name) && x.Properties.Any(p => p.Key == "crate.version" && p.Value == component.Version));

            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqllist);

            return repoName;
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

        #endregion
    }
}
