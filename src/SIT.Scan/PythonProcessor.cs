// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using CycloneDX.Models;
using log4net;
using SIT.APICommunications;
using SIT.APICommunications.Model.AQL;
using SIT.Common;
using SIT.Common.Constants;
using SIT.Common.Interface;
using SIT.Scan.Interface;
using SIT.Scan.Model;
using SIT.Services.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Tommy;
using Component = CycloneDX.Models.Component;
using Dependency = CycloneDX.Models.Dependency;

namespace SIT.Scan
{
    public class PythonProcessor(ICycloneDXBomParser cycloneDXBomParser, ISpdxBomParser spdxBomParser) : IParser
    {
        private const string NotFoundInRepo = "Not Found in JFrogRepo";

        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICycloneDXBomParser _cycloneDXBomParser = cycloneDXBomParser;
        private readonly ISpdxBomParser _spdxBomParser = spdxBomParser;
        private static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
        private const string devGroupName = "dev";
        private const string mainGroupName = "main";

        // TOML key names used in poetry.lock / uv.lock files.
        private const string TomlKeyPackage = "package";
        private const string TomlKeyName = "name";
        private const string TomlKeyVersion = "version";
        private const string TomlKeySource = "source";
        private const string TomlKeyMetadata = "metadata";
        private const string TomlKeyDependencies = "dependencies";
        private const string TomlKeyDevDependencies = "dev-dependencies";
        private const string TomlKeyRequiresDev = "requires-dev";
        private const string TomlKeyIncludeGroup = "include-group";
        private const string TomlKeySourceVirtual = "virtual";
        private const string TomlKeySourceEditable = "editable";
        private const string TomlKeyGroups = "groups";
        private const string TomlKeyCategory = "category";

        // Placeholder value returned by the Tommy TOML library when a key is absent.
        private const string TommyLazyPlaceholder = "Tommy.TomlLazy";

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
            List<string> configFiles = FolderScanner.FileScanner(appSettings.Directory.InputFolder, appSettings.Poetry, environmentHelper);
            List<PythonPackage> listofComponents;
            Bom bom = new Bom();
            List<Component> listComponentForBOM = new List<Component>();
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
                    listofComponents = ExtractDetailsForPoetryLockfile(config, ListofDependenciesFromLockFile);
                    ListofComponentsFromLockFile.AddRange(FormComponentReleaseExternalID(listofComponents));

                }
                else if (config.ToLower().EndsWith("uv.lock"))
                {
                    Logger.DebugFormat("ParsePackageFile():uv lock file detected: {0}", config);
                    listofComponents = ExtractDetailsForUvLockfile(config, ListofDependenciesFromLockFile);
                    ListofComponentsFromLockFile.AddRange(FormComponentReleaseExternalID(listofComponents));
                }
                else if ((config.EndsWith(FileConstant.CycloneDXFileExtension) || config.EndsWith(FileConstant.DependencyFileExtension) || config.EndsWith(FileConstant.SPDXFileExtension))
         && !config.EndsWith(FileConstant.SBOMTemplateFileExtension))
                {
                    listofComponents = ExtractDetailsFromJson(config, appSettings, ref dependencies);
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
        /// Extract Details For uv Lock file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="dependencies"></param>
        /// <returns>list of package</returns>
        public static List<PythonPackage> ExtractDetailsForUvLockfile(string filePath, List<Dependency> dependencies)
        {
            List<PythonPackage> PythonPackages;
            PythonPackages = GetPackagesFromUvTomlFile(filePath, dependencies);
            IdentifiedPythonPackages(filePath, PythonPackages);
            return PythonPackages;
        }

        /// <summary>
        /// Gets Packages From uv.lock TOML File
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="dependencies"></param>
        /// <returns>list of package</returns>
        private static List<PythonPackage> GetPackagesFromUvTomlFile(string filePath, List<Dependency> dependencies)
        {
            List<PythonPackage> PythonPackages = new();
            List<KeyValuePair<string, TomlNode>> keyValuePair = new();
            FileParser fileParser = new();
            TomlTable tomlTable = fileParser.ParseTomlFile(filePath);
            CommonHelper.WarnIfDependencyFileRequired();

            // Index every non-root package node by normalized name and capture the root
            // (virtual/editable) project node in a single pass over the lock file.
            Dictionary<string, TomlNode> nodesByName = new(StringComparer.OrdinalIgnoreCase);
            TomlNode rootNode = IndexUvPackages(tomlTable, nodesByName);

            // Dev names declared in the root project's dev groups, then the full set of
            // runtime- and dev-reachable packages computed as transitive closures over
            // the dependency graph. Runtime reachability takes precedence over dev.
            HashSet<string> declaredDevNames = BuildUvDevDependencySet(rootNode);
            HashSet<string> runtimeNames = CollectReachableNames(GetUvNodeDependencyNames(rootNode), nodesByName);
            HashSet<string> devNames = CollectReachableNames(declaredDevNames, nodesByName);

            foreach (TomlNode node in tomlTable[TomlKeyPackage])
            {
                // The virtual/editable root project represents the project itself,
                // not a third-party dependency, so it is excluded from the BOM.
                if (IsUvRootProject(node))
                {
                    continue;
                }

                string packageName = node[TomlKeyName].ToString();
                string packageVersion = node.TryGetNode(TomlKeyVersion, out TomlNode versionNode) ? versionNode.ToString() : string.Empty;
                string normalizedName = CommonHelper.NormalizePypiName(packageName);

                bool isDev = devNames.Contains(normalizedName) && !runtimeNames.Contains(normalizedName);

                PythonPackage pythonPackage = new()
                {
                    Name = packageName,
                    Version = packageVersion,
                    PurlID = GeneratePypiPurl(packageName, packageVersion, Dataconstant.PoetryProjectType),
                    Isdevdependent = isDev,
                    FoundType = Dataconstant.Discovered,
                    SpdxComponentDetails = new SpdxComponentInfo()
                };

                BomCreator.bomKpiData.ComponentsinPackageLockJsonFile++;
                if (isDev)
                {
                    BomCreator.bomKpiData.DevDependentComponents++;
                }

                PythonPackages.Add(pythonPackage);
                keyValuePair.Add(new KeyValuePair<string, TomlNode>(pythonPackage.PurlID, node));
            }

            BuildUvDependencyGraph(keyValuePair, dependencies, PythonPackages);

            return PythonPackages;
        }

        /// <summary>
        /// Indexes every non-root uv package node by its normalized name and returns the
        /// root (virtual/editable) project node, or null when none is present.
        /// </summary>
        /// <param name="tomlTable">Parsed uv.lock table.</param>
        /// <param name="nodesByName">Accumulator mapping normalized package name to its node.</param>
        /// <returns>The root project node, or null.</returns>
        private static TomlNode IndexUvPackages(TomlTable tomlTable, Dictionary<string, TomlNode> nodesByName)
        {
            TomlNode rootNode = null;
            foreach (TomlNode node in tomlTable[TomlKeyPackage])
            {
                if (IsUvRootProject(node))
                {
                    rootNode = node;
                    continue;
                }

                string name = node.TryGetNode(TomlKeyName, out TomlNode nameNode) ? nameNode.ToString() : null;
                if (!string.IsNullOrEmpty(name))
                {
                    nodesByName[CommonHelper.NormalizePypiName(name)] = node;
                }
            }

            return rootNode;
        }

        /// <summary>
        /// Builds the set of development dependency names from the uv.lock root project.
        /// The root project is the [[package]] whose source is virtual/editable. Development
        /// dependencies are declared either under [package.dev-dependencies] (the format
        /// emitted by current uv versions) or under [package.metadata].requires-dev (used by
        /// older/legacy representations); both are supported for compatibility.
        /// Group entries may reference other groups via "include-group"; such nested
        /// groups are resolved recursively per the uv specification.
        /// </summary>
        /// <param name="rootNode">The root (virtual/editable) project node, may be null.</param>
        /// <returns>Set of normalized development dependency names.</returns>
        private static HashSet<string> BuildUvDevDependencySet(TomlNode rootNode)
        {
            HashSet<string> devDependencyNames = new(StringComparer.OrdinalIgnoreCase);
            if (rootNode == null)
            {
                return devDependencyNames;
            }

            // Map each dev group name to its array of entries so that
            // "include-group" references can be resolved across groups. Collect from
            // both the current [package.dev-dependencies] table and the legacy
            // [package.metadata].requires-dev table.
            Dictionary<string, TomlNode> groupsByName = new(StringComparer.OrdinalIgnoreCase);
            foreach (var devDependenciesTable in new[] { rootNode[TomlKeyDevDependencies], rootNode[TomlKeyMetadata]?[TomlKeyRequiresDev] })
            {
                AddUvDevGroups(devDependenciesTable, groupsByName);
            }

            foreach (var groupName in groupsByName.Keys)
            {
                CollectUvGroupDependencyNames(groupName, groupsByName, devDependencyNames, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }

            return devDependencyNames;
        }

        /// <summary>
        /// Computes the transitive closure of package names reachable from a set of seed
        /// names by walking the uv dependency graph. Used for both the runtime closure
        /// (seeded from the root's dependencies) and the dev closure (seeded from the
        /// declared dev names); a single shared walk keeps the two classifications consistent.
        /// </summary>
        /// <param name="seedNames">Normalized names to start the walk from.</param>
        /// <param name="nodesByName">All non-root package nodes keyed by normalized name.</param>
        /// <returns>Set of normalized names reachable from the seeds (including the seeds).</returns>
        private static HashSet<string> CollectReachableNames(IEnumerable<string> seedNames, Dictionary<string, TomlNode> nodesByName)
        {
            HashSet<string> reachable = new(StringComparer.OrdinalIgnoreCase);
            foreach (string seed in seedNames)
            {
                CollectReachableNamesRecursively(seed, nodesByName, reachable);
            }

            return reachable;
        }

        /// <summary>
        /// Adds a package and all of its transitive dependencies to the reachable set.
        /// The set itself guards against cycles and repeated visits.
        /// </summary>
        private static void CollectReachableNamesRecursively(string packageName, Dictionary<string, TomlNode> nodesByName, HashSet<string> reachable)
        {
            if (string.IsNullOrEmpty(packageName) || !reachable.Add(packageName) || !nodesByName.TryGetValue(packageName, out TomlNode node))
            {
                return;
            }

            foreach (string childName in GetUvNodeDependencyNames(node))
            {
                CollectReachableNamesRecursively(childName, nodesByName, reachable);
            }
        }

        /// <summary>
        /// Enumerates the normalized names declared in a uv package node's
        /// dependencies array (dependencies = [{ name = "..." }]).
        /// </summary>
        private static IEnumerable<string> GetUvNodeDependencyNames(TomlNode node)
        {
            var dependencies = node[TomlKeyDependencies];
            if (dependencies == null || !dependencies.IsArray)
            {
                yield break;
            }

            foreach (TomlNode dependency in dependencies.AsArray.Children)
            {
                string childName = CommonHelper.NormalizePypiName(dependency?[TomlKeyName]?.ToString());
                if (!string.IsNullOrEmpty(childName))
                {
                    yield return childName;
                }
            }
        }

        /// <summary>
        /// Adds dev dependency groups from a uv dev-dependencies table (either
        /// [package.dev-dependencies] or the legacy [package.metadata].requires-dev)
        /// into the supplied group map. Groups already present are not overwritten,
        /// but their entries are merged so declarations from both tables are honored.
        /// </summary>
        /// <param name="devDependenciesTable">The dev-dependencies table node, may be null.</param>
        /// <param name="groupsByName">Accumulator mapping group name to its entry array.</param>
        private static void AddUvDevGroups(TomlNode devDependenciesTable, Dictionary<string, TomlNode> groupsByName)
        {
            if (devDependenciesTable == null || !devDependenciesTable.IsTable)
            {
                return;
            }

            foreach (var group in devDependenciesTable.AsTable.RawTable)
            {
                if (group.Value == null || !group.Value.IsArray)
                {
                    continue;
                }

                if (!groupsByName.TryGetValue(group.Key, out TomlNode existing))
                {
                    groupsByName[group.Key] = group.Value;
                    continue;
                }

                // Merge entries when the same group name appears in both tables.
                foreach (TomlNode entry in group.Value.AsArray.Children)
                {
                    existing.AsArray.Add(entry);
                }
            }
        }

        /// <summary>
        /// Recursively collects normalized dependency names from a uv dev group,
        /// resolving nested "include-group" references while guarding against cycles.
        /// </summary>
        /// <param name="groupName">Name of the group to collect from.</param>
        /// <param name="groupsByName">All dev groups keyed by name.</param>
        /// <param name="devDependencyNames">Accumulator for normalized dependency names.</param>
        /// <param name="visitedGroups">Groups already visited to avoid infinite recursion.</param>
        private static void CollectUvGroupDependencyNames(string groupName, Dictionary<string, TomlNode> groupsByName, HashSet<string> devDependencyNames, HashSet<string> visitedGroups)
        {
            if (!visitedGroups.Add(groupName) || !groupsByName.TryGetValue(groupName, out TomlNode groupEntries))
            {
                return;
            }

            foreach (TomlNode entry in groupEntries.AsArray.Children)
            {
                CollectUvGroupEntry(entry, groupsByName, devDependencyNames, visitedGroups);
            }
        }

        /// <summary>
        /// Processes a single uv dev-group entry: either resolves a nested
        /// "include-group" reference or records a normalized dependency name.
        /// </summary>
        private static void CollectUvGroupEntry(TomlNode entry, Dictionary<string, TomlNode> groupsByName, HashSet<string> devDependencyNames, HashSet<string> visitedGroups)
        {
            if (entry == null || !entry.IsTable)
            {
                return;
            }

            if (entry.AsTable.HasKey(TomlKeyIncludeGroup))
            {
                var includeGroup = entry[TomlKeyIncludeGroup]?.ToString();
                if (!string.IsNullOrEmpty(includeGroup))
                {
                    CollectUvGroupDependencyNames(includeGroup, groupsByName, devDependencyNames, visitedGroups);
                }
                return;
            }

            if (entry.AsTable.HasKey(TomlKeyName))
            {
                var name = entry[TomlKeyName]?.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    devDependencyNames.Add(CommonHelper.NormalizePypiName(name));
                }
            }
        }

        /// <summary>
        /// Determines whether a uv.lock package node is the root (virtual/editable) project.
        /// </summary>
        /// <param name="node">Package node.</param>
        /// <returns>True if the node represents the root project.</returns>
        private static bool IsUvRootProject(TomlNode node)
        {
            var source = node[TomlKeySource];
            if (source == null || !source.IsTable)
            {
                return false;
            }

            return source.AsTable.HasKey(TomlKeySourceVirtual) || source.AsTable.HasKey(TomlKeySourceEditable);
        }

        /// <summary>
        /// Builds the CycloneDX dependency graph from uv.lock package dependency arrays.
        /// uv represents dependencies as an array of inline tables: dependencies = [{ name = "..." }].
        /// </summary>
        /// <param name="keyValues">Purl-to-node pairs.</param>
        /// <param name="dependencies">Dependency list to populate.</param>
        /// <param name="PythonPackages">Parsed packages used for version resolution.</param>
        private static void BuildUvDependencyGraph(List<KeyValuePair<string, TomlNode>> keyValues, List<Dependency> dependencies, List<PythonPackage> PythonPackages)
        {
            foreach (var node in keyValues)
            {
                var dep = node.Value[TomlKeyDependencies];
                List<Dependency> subDependencies = new();
                if (dep != null && dep.IsArray)
                {
                    foreach (TomlNode dependency in dep.AsArray)
                    {
                        var name = dependency?[TomlKeyName]?.ToString();
                        if (!string.IsNullOrEmpty(name))
                        {
                            subDependencies.Add(new Dependency()
                            {
                                Ref = ResolveUvDependencyRef(name, PythonPackages)
                            });
                        }
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
        /// Forms a purl ref for a uv dependency by resolving its version from the parsed packages.
        /// Names are normalized before matching to avoid case/underscore mismatches.
        /// </summary>
        /// <param name="name">Dependency name.</param>
        /// <param name="PythonPackages">Parsed packages.</param>
        /// <returns>The purl ref.</returns>
        private static string ResolveUvDependencyRef(string name, List<PythonPackage> PythonPackages)
        {
            var normalized = CommonHelper.NormalizePypiName(name);
            var version = PythonPackages.Find(val => CommonHelper.NormalizePypiName(val.Name) == normalized)?.Version;

            if (string.IsNullOrEmpty(version))
            {
                return GeneratePypiPurl(name, "*", Dataconstant.PoetryProjectType);
            }

            return GeneratePypiPurl(name, version, Dataconstant.PoetryProjectType);
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
            foreach (TomlNode node in tomlTable[TomlKeyPackage])
            {
                PythonPackage pythonPackage = new()
                {
                    Name = node[TomlKeyName].ToString(),
                    Version = node[TomlKeyVersion].ToString(),
                    PurlID = GeneratePypiPurl(node[TomlKeyName].ToString(), node[TomlKeyVersion].ToString(), Dataconstant.PoetryProjectType),
                    // Support both Poetry 1.x (category) and Poetry 2.x (groups)
                    Isdevdependent = IsDevDependency(node),
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
        /// Determines if a package is a development dependency based on Poetry version
        /// Supports both Poetry 1.x (category field) and Poetry 2.x (groups field)
        /// </summary>
        /// <param name="node">TOML node containing package information</param>
        /// <returns>True if package is a development dependency, false otherwise</returns>
        private static bool IsDevDependency(TomlNode node)
        {
            // Poetry 2.x uses 'groups' field
            if (node.HasKey(TomlKeyGroups))
            {
                var groupsNode = node[TomlKeyGroups];
                if (groupsNode != null && !string.Equals(groupsNode.ToString(), TommyLazyPlaceholder, StringComparison.Ordinal))
                {
                    // Check if any group in the list is a development group
                    return ContainsDevGroups(groupsNode);
                }
            }

            // Poetry 1.x uses 'category' field (fallback for backward compatibility)
            if (node.HasKey(TomlKeyCategory))
            {
                string category = node[TomlKeyCategory]?.ToString() ?? string.Empty;
                return !string.Equals(category, mainGroupName, StringComparison.Ordinal) &&
                       !string.Equals(category, TommyLazyPlaceholder, StringComparison.Ordinal);
            }

            // Default to production dependency if neither field is present or recognizable
            return false;
        }

        /// <summary>
        /// Checks if the groups contain development-related groups
        /// </summary>
        /// <param name="groupsNode">Groups node from Poetry 2.x (can be array or string)</param>
        /// <returns>True if contains dev groups, false otherwise</returns>
        private static bool ContainsDevGroups(TomlNode groupsNode)
        {
            // Handle groups as array (most common case)
            if (groupsNode.IsArray)
            {
                return ContainsDevGroupsInArray(groupsNode.AsArray);
            }
            // Handle groups as single string (fallback)
            else
            {
                return IsDevGroupString(groupsNode.ToString());
            }
        }

        /// <summary>
        /// contains dev groups in array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private static bool ContainsDevGroupsInArray(Tommy.TomlArray array)
        {
            bool hasDev = false;
            bool hasMain = false;

            foreach (var groupItem in array)
            {
                string groupName = groupItem?.ToString()?.Trim();
                if (string.IsNullOrEmpty(groupName))
                    continue;

                if (string.Equals(groupName, mainGroupName, StringComparison.OrdinalIgnoreCase))
                    hasMain = true;

                if (string.Equals(groupName, devGroupName, StringComparison.OrdinalIgnoreCase))
                    hasDev = true;
            }
            return hasDev && !hasMain;
        }

        /// <summary>
        /// Contains dev groups as string 
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        private static bool IsDevGroupString(string groupName)
        {
            groupName = groupName?.Trim();
            if (!string.IsNullOrEmpty(groupName))
            {
                return string.Equals(groupName, devGroupName, StringComparison.OrdinalIgnoreCase);
            }
            return false;
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
                var dep = node.Value[TomlKeyDependencies];
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
                return GeneratePypiPurl(valuePair.Key, "*", Dataconstant.PoetryProjectType);
            }
            else
            {
                return GeneratePypiPurl(valuePair.Key, value, Dataconstant.PoetryProjectType);
            }
        }

        /// <summary>
        /// Generates a spec-compliant PyPI (Poetry) package-url from a component name and version
        /// using the packageurl-dotnet library (via the common GeneratePurl helper).
        /// Per the package-url spec, the pypi type lowercases the name and replaces underscores with
        /// hyphens (e.g. "MarkupSafe" =&gt; "markupsafe").
        /// </summary>
        /// <param name="name">The component name.</param>
        /// <param name="version">The component version.</param>
        /// <param name="projectType">The project type used to resolve the purl type.</param>
        /// <returns>A generated, spec-compliant pypi purl string.</returns>
        private static string GeneratePypiPurl(string name, string version, string projectType)
        {
            string projectTypeKey = string.IsNullOrWhiteSpace(projectType) ? Dataconstant.PoetryProjectType : projectType;
            return CommonHelper.GeneratePurlForProjectType(projectTypeKey, name, version);
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
                if (!string.IsNullOrEmpty(componentsInfo.Name) && !string.IsNullOrEmpty(componentsInfo.Version))
                {
                    // Always generate a correct purl from name and version; do not reuse the incoming purl.
                    package.PurlID = GeneratePypiPurl(componentsInfo.Name, componentsInfo.Version, Dataconstant.PoetryProjectType);
                }
                SetSpdxComponentDetails(filePath, package, componentsInfo);

                if (!string.IsNullOrEmpty(package.Name) && !string.IsNullOrEmpty(package.Version) && !string.IsNullOrEmpty(package.PurlID) && package.PurlID.Contains(Dataconstant.PurlCheck()[Dataconstant.PoetryProjectType]))
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
        /// Form Component Release ExternalID
        /// </summary>
        /// <param name="listOfComponents"></param>
        /// <returns>list of components</returns>
        private static List<Component> FormComponentReleaseExternalID(List<PythonPackage> listOfComponents)
        {
            List<Component> listComponentForBOM = new List<Component>();

            foreach (var prop in listOfComponents)
            {
                // Reuse the purl already generated during parsing; no need to regenerate.
                Component component = CommonHelper.CreateComponentWithProperties(
                    prop.Name,
                    prop.Version,
                    prop.PurlID
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
            string normalizedComponentName = CommonHelper.NormalizePypiName(jfrogcomponentName);

            if (aqlResultList.Exists(x => x.Properties.Any(p => p.Key == Dataconstant.PypiNormalizedNameKey && string.Equals(CommonHelper.NormalizePypiName(p.Value), normalizedComponentName, StringComparison.Ordinal)) && x.Properties.Any(p => p.Key == Dataconstant.PypiVersionKey && p.Value == component.Version)))
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
            string normalizedName = CommonHelper.NormalizePypiName(name);

            string nameVerison = aqlResultList.FirstOrDefault(x =>
                x.Properties.Any(p => p.Key == Dataconstant.PypiNormalizedNameKey
                    && string.Equals(CommonHelper.NormalizePypiName(p.Value), normalizedName, StringComparison.Ordinal))
                && x.Properties.Any(p => p.Key == Dataconstant.PypiVersionKey && p.Value == version))?.Name ?? string.Empty;

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

            List<AqlResult> internalAqlResultList = CommonIdentiferHelper.FilterAqlResultsByRepos(aqlResultList, CommonIdentiferHelper.GetInternalRepos(appSettings));

            foreach (var component in componentsForBOM)
            {
                var componentAqlResultList = CommonIdentiferHelper.GetAqlResultsForComponent(component, aqlResultList, internalAqlResultList);
                var processedComponent = ProcessPythonComponent(component, componentAqlResultList, bomhelper, appSettings, projectType);
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

            string normalizedComponentName = CommonHelper.NormalizePypiName(component.Name);
            var hashes = aqlResultList.FirstOrDefault(x => x.Properties.Any(p => p.Key == Dataconstant.PypiNormalizedNameKey && string.Equals(CommonHelper.NormalizePypiName(p.Value), normalizedComponentName, StringComparison.Ordinal)) && x.Properties.Any(p => p.Key == Dataconstant.PypiVersionKey && p.Value == component.Version));

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

            string repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults, component);

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
                    repoName = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqllist, component);
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
