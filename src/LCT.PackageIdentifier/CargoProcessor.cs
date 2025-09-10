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
                if (filepath.ToLower().EndsWith(FileConstant.CargoLockFile))
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

            List<Component> cargoPackages = GetAllPackagesFromCargoLock(filePath, out var keyValuePair, out var packageVersionLookup);

            string tomlPath = Path.Combine(Path.GetDirectoryName(filePath)!, FileConstant.CargoTomlFile);
            if (!File.Exists(tomlPath))
            {
                Logger.Warn($"Cargo.toml file not found at path: {tomlPath}. Direct and Dev dependencies will not be identified.");
                GetCargoRefDetailsFromDependencyText(keyValuePair, dependencies, cargoPackages, packageVersionLookup, new HashSet<(string, string)>());
                Logger.Debug("[GetPackagesFromCargoLockFile] Finished processing Cargo.lock (without Cargo.toml).");
                return cargoPackages;
            }

            var (directDepsDict, devDepsDict, buildDepsDict) = ParseCrateDependenciesWithWorkspaceVersions(tomlPath, tomlPath);

            var directDeps = new HashSet<(string, string)>();
            var devDeps = new HashSet<(string, string)>();
            var buildDeps = new HashSet<(string, string)>();
            foreach (var pkg in cargoPackages)
            {
                if (directDepsDict.TryGetValue(pkg.Name, out var ver) && ver == pkg.Version)
                    directDeps.Add((pkg.Name, pkg.Version));
                if (devDepsDict.TryGetValue(pkg.Name, out var ver2) && ver2 == pkg.Version)
                    devDeps.Add((pkg.Name, pkg.Version));
                if (buildDepsDict.TryGetValue(pkg.Name, out var ver3) && ver3 == pkg.Version)
                    buildDeps.Add((pkg.Name, pkg.Version));
            }

            GetCargoRefDetailsFromDependencyText(keyValuePair, dependencies, cargoPackages, packageVersionLookup, directDeps);
            MarkDevAndBuildDependenciesAccurately(cargoPackages, directDeps, devDeps, buildDeps, dependencies);

            Logger.Debug("[GetPackagesFromCargoLockFile] Finished processing Cargo.lock.");
            return cargoPackages;
        }
        private static (Dictionary<string, string> directDeps,Dictionary<string, string> devDeps,Dictionary<string, string> buildDeps) ParseCrateDependenciesWithWorkspaceVersions(string crateTomlPath, string workspaceTomlPath)
        {
            var directDeps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var devDeps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var buildDeps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var workspaceVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 1. Parse [workspace.dependencies] for version lookup
            if (File.Exists(workspaceTomlPath))
            {
                using var reader = new StreamReader(workspaceTomlPath);
                TomlTable wsTable = TOML.Parse(reader);
                if (wsTable.HasKey("workspace") && wsTable["workspace"] is TomlTable workspaceTable)
                {
                    if (workspaceTable.HasKey("dependencies") && workspaceTable["dependencies"] is TomlTable wsDepsTable)
                    {
                        foreach (var key in wsDepsTable.Keys)
                        {
                            var depNode = wsDepsTable[key];
                            string version;
                            if (depNode is TomlTable depTable && depTable.HasKey("version"))
                                version = depTable["version"].ToString();
                            else
                                version = depNode.ToString();

                            workspaceVersions[key] = version;
                        }
                    }
                }
            }

            if (File.Exists(crateTomlPath))
            {
                using var reader = new StreamReader(crateTomlPath);
                TomlTable crateTable = TOML.Parse(reader);
                static string GetVersion(TomlNode node)
                {
                    if (node is TomlTable tbl && tbl.HasKey("version"))
                        return tbl["version"].ToString();
                    return node.ToString();
                }

                if (crateTable.HasKey("dependencies") && crateTable["dependencies"] is TomlTable depsTable)
                {
                    foreach (var key in depsTable.Keys)
                    {
                        if (workspaceVersions.TryGetValue(key, out var wsVersion))
                            directDeps[key] = wsVersion;
                        else
                            directDeps[key] = GetVersion(depsTable[key]);
                    }
                }
                if (crateTable.HasKey("dev-dependencies") && crateTable["dev-dependencies"] is TomlTable devDepsTable)
                {
                    foreach (var key in devDepsTable.Keys)
                    {
                        if (workspaceVersions.TryGetValue(key, out var wsVersion))
                            devDeps[key] = wsVersion;
                        else
                            devDeps[key] = GetVersion(devDepsTable[key]);
                    }
                }
                if (crateTable.HasKey("build-dependencies") && crateTable["build-dependencies"] is TomlTable buildDepsTable)
                {
                    foreach (var key in buildDepsTable.Keys)
                    {
                        if (workspaceVersions.TryGetValue(key, out var wsVersion))
                            buildDeps[key] = wsVersion;
                        else
                            buildDeps[key] = GetVersion(buildDepsTable[key]);
                    }
                }
            }

            return (directDeps, devDeps, buildDeps);
        }
        private static void MarkDevAndBuildDependenciesAccurately(List<Component> cargoPackages,HashSet<(string, string)> directDeps,HashSet<(string, string)> devDeps,HashSet<(string, string)> buildDeps,List<Dependency> allDependencies)
        {
            // 1. Build a map from package name to Dependency object
            var depMap = allDependencies.ToDictionary(d => d.Ref, d => d);

            // 2. Find all packages reachable from direct dependencies (transitive closure)
            var usedByDirect = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Traverse(string depRef)
            {
                if (!usedByDirect.Add(depRef)) return; // already visited
                if (depMap.TryGetValue(depRef, out var dep) && dep.Dependencies != null)
                {
                    foreach (var sub in dep.Dependencies)
                        Traverse(sub.Ref);
                }
            }

            foreach (var direct in cargoPackages.Where(c => directDeps.Contains((c.Name, c.Version))))
                Traverse(direct.Purl);

            // 3. Mark dev/build dependencies only if NOT used by direct dependencies
            foreach (var component in cargoPackages)
            {
                bool isDirect = directDeps.Contains((component.Name, component.Version));
                bool isDev = devDeps.Contains((component.Name, component.Version)) && !usedByDirect.Contains(component.Purl);
                bool isBuild = buildDeps.Contains((component.Name, component.Version)) && !usedByDirect.Contains(component.Purl);
                var properties = component.Properties ??= new List<Property>();
                CommonHelper.RemoveDuplicateAndAddProperty(ref properties,
                    Dataconstant.Cdx_IsDevelopment, isDev || isBuild ? "true" : "false");
                component.Properties = properties;
                if (isDev || isBuild)
                {                    
                    BomCreator.bomKpiData.DevDependentComponents++;
                }
            }
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

        private static void GetCargoRefDetailsFromDependencyText(
     List<KeyValuePair<string, TomlNode>> keyValues,
     List<Dependency> dependencies,
     List<Component> cargoPackages,
     Dictionary<string, string> packageVersionLookup,
     HashSet<(string, string)> directDeps)
        {
            foreach (var node in keyValues)
            {
                // Extract the package name and version from the PURL
                string packageName = "";
                string packageVersion = "";
                var purlParts = node.Key.Split('/');
                if (purlParts.Length > 1)
                {
                    var nameAndVersion = purlParts[^1].Split('@');
                    if (nameAndVersion.Length > 0)
                        packageName = nameAndVersion[0];
                    if (nameAndVersion.Length > 1)
                        packageVersion = nameAndVersion[1];
                }

                // Only add if this is a direct dependency (by name and version)
                if (!directDeps.Contains((packageName, packageVersion)))
                    continue;

                var dep = node.Value["dependencies"];
                List<Dependency> subDependencies = new();
                if (dep != null && dep.ChildrenCount > 0)
                {
                    foreach (var dependency in dep)
                    {
                        string depStr = dependency.ToString();
                        var parts = depStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        string depName = parts[0];
                        string depVersion = parts.Length > 1 ? parts[1] : null;

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
