// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

// Ignore Spelling: Canonicalize

using CycloneDX.Models;
using LCT.Common.Constants;
using LCT.Common.Model;
using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using File = System.IO.File;

namespace LCT.Common
{
    /// <summary>
    /// Common Helper class
    /// </summary>
    public static class CommonHelper
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly char[] InvalidProjectNameChars = new char[] { '/', '\\', '.' };
        public static string ProjectSummaryLink { get; set; }
        public static string DefaultLogPath { get; set; }

        public static bool DependencyFileNotFound { get; set; }=true;

        #region public
        public static bool IsAzureDevOpsDebugEnabled()
        {
            string azureDevOpsDebug = System.Environment.GetEnvironmentVariable("System.Debug");
            if (bool.TryParse(azureDevOpsDebug, out bool systemDebugEnabled) && systemDebugEnabled)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Finds and parses a cdxgen-generated CycloneDX SBOM file if present in the provided config files.
        /// The detected dependency file path is removed from the list to avoid further processing elsewhere.
        /// Parsing is performed by the provided delegate to avoid introducing cross-project dependencies.
        /// </summary>
        /// <param name="configFiles">List of config file paths (will be mutated if dependency file is found)</param>
        /// <param name="appSettings">Application settings</param>
        /// <param name="parseCycloneDxBom">Delegate used to parse the CycloneDX file and return a Bom</param>
        /// <returns>Parsed Bom or null when not found/invalid</returns>
        public static Bom GetCdxGenBomData(List<string> configFiles, Func<string, Bom> parseCycloneDxBom)
        {
            var dependencyFilePath = configFiles
                .FirstOrDefault(f => f.EndsWith(FileConstant.DependencyFileExtension, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(dependencyFilePath))
            {
                DependencyFileNotFound = false;
                return null;
            }

            bool onlyDependencyFiles = configFiles.Count > 0 &&
                configFiles.All(f => f.EndsWith(FileConstant.DependencyFileExtension, StringComparison.OrdinalIgnoreCase));

            if (onlyDependencyFiles)
            {
                DependencyFileNotFound = false;
                return null;
            }

            configFiles.Remove(dependencyFilePath);

            
            var cdxGenBomData = parseCycloneDxBom?.Invoke(dependencyFilePath);

            if (cdxGenBomData?.Components != null)
            {
                return cdxGenBomData;
            }

            return null;
        }

        /// <summary>
        /// Merge cdxgen output into the in-memory lists or fall back to discovered lock-file content.
        /// If cdxGenBomData has components, ApplyCdxGenEnrichment is used. Otherwise, discovered
        /// components and dependencies are appended to the BOM lists.
        /// </summary>
        public static void EnrichCdxGenforPackagefilesData(
            ref List<Component> listOfComponentsFromLockFile,
            ref List<Dependency> listOfDependenciesFromLockFile,
            ref List<Component> componentsForBOM,
            ref List<Dependency> dependencies,
            Bom cdxGenBomData)
        {
            if (cdxGenBomData?.Components == null || cdxGenBomData.Components.Count == 0)
            {
                componentsForBOM.AddRange(listOfComponentsFromLockFile);
                dependencies.AddRange(listOfDependenciesFromLockFile);
            }
            else
            {
                ApplyCdxGenEnrichment(
                    ref listOfComponentsFromLockFile,
                    ref listOfDependenciesFromLockFile,
                    ref componentsForBOM,
                    ref dependencies,
                    cdxGenBomData);
            }
        }
        /// <summary>
        /// Logs a warning asking for cdxgen-generated SBOM data, controlled by the DependencyFileGiven flag.
        /// Call this when you want to prompt the user to provide the dependency SBOM.
        /// </summary>
        public static void WarnIfDependencyFileRequired()
        {
            if (!DependencyFileNotFound)
            {
                DependencyFileNotFound=true;
                Logger.Warn("   Can you please provide the cdxgen-generated SBOM to get more accurate dependencies");
            }
        }
        public static List<Component> RemoveExcludedComponents(List<Component> ComponentList, List<string> ExcludedComponents, ref int noOfExcludedComponents)
        {
            List<string> ExcludedComponentsFromPurl = ExcludedComponents?.Where(ec => ec.StartsWith("pkg:")).ToList();
            List<string> otherExcludedComponents = ExcludedComponents?.Where(ec => !ec.StartsWith("pkg:")).ToList();

            AddExcludedComponentsPropertyFromPurl(ComponentList, ExcludedComponentsFromPurl, ref noOfExcludedComponents);
            AddExcludedComponentsPropertyFromNameAndVersion(ComponentList, otherExcludedComponents, ref noOfExcludedComponents);
            return ComponentList;
        }

        public static List<Dependency> RemoveInvalidDependenciesAndReferences(List<Component> components, List<Dependency> dependencies)
        {
            var componentBomRefs = new HashSet<string>(components.Select(c => c.BomRef));

            dependencies.RemoveAll(dep => !componentBomRefs.Contains(dep.Ref));

            foreach (var dep in dependencies)
            {
                dep.Dependencies?.RemoveAll(refItem => !componentBomRefs.Contains(refItem.Ref));
            }

            return dependencies;
        }

        public static string GetSubstringOfLastOccurance(string value, string separator)
        {
            string result = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
            if (result.Contains(separator))
            {
                result = result[(result.LastIndexOf(separator) + separator.Length)..];
            }

            return result;
        }
        public static string TrimEndOfString(this string input, string suffixToRemove, StringComparison comparisonType = StringComparison.CurrentCulture)
        {
            if (suffixToRemove != null && input.EndsWith(suffixToRemove, comparisonType))
            {
                return input.Substring(0, input.Length - suffixToRemove.Length);
            }

            return input;
        }

        /// <summary>
        /// Get display name for given instance type and property name
        /// </summary>
        /// <param name="objectValue">pass the object</param>
        /// <param name="nameOfProperty">Property</param>
        /// <returns>string</returns>
        public static string Convert(object objectValue, object nameOfProperty)
        {
            var attribute = objectValue.GetType()
                .GetProperty(nameOfProperty.ToString())
                .GetCustomAttributes(false)
                .OfType<System.ComponentModel.DisplayNameAttribute>()
                .FirstOrDefault();

            return attribute != null ? attribute.DisplayName : string.Empty;
        }

        public static void CheckNullOrEmpty(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Invalid value for {name} - {value}");
            }
        }

        public static bool ComponentPropertyCheck(Component component, string constant)
        {
            if (component.Properties == null)
            {
                return false;
            }
            return component.Properties.Exists(x => x.Name == constant);
        }

        public static void GetDetailsForManuallyAdded(List<Component> componentsForBOM, List<Component> listComponentForBOM, string filePath)
        {
            foreach (var component in componentsForBOM)
            {
                string fileName = Path.GetFileName(filePath);
                component.Properties ??= new List<Property>();

                if (filePath.EndsWith(FileConstant.SPDXFileExtension))
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
                        "false");
                    component.Properties = properties;
                }
                listComponentForBOM.Add(component);
            }
        }

        public static string AddSpecificValuesToBOMFormat(Bom listOfComponentsToBom)
        {
            string guid = Guid.NewGuid().ToString();
            listOfComponentsToBom.SerialNumber = $"urn:uuid:{guid}";
            listOfComponentsToBom.Version = 1;
            listOfComponentsToBom.Metadata.Timestamp = DateTime.UtcNow;
            listOfComponentsToBom.SpecVersion = CycloneDX.SpecificationVersion.v1_6;
            listOfComponentsToBom.SpecVersionString = Dataconstant.SbomSpecVersionString;
            var serializedBomData = CycloneDX.Json.Serializer.Serialize(listOfComponentsToBom);
            return serializedBomData;
        }
        public static string[] GetRepoList(CommonAppSettings appSettings)
        {
            var projectTypeMappings = new Dictionary<string, Func<Config>>
    {
        { "CONAN", () => appSettings.Conan },
        { "NPM", () => appSettings.Npm },
        { "NUGET", () => appSettings.Nuget },
        { "POETRY", () => appSettings.Poetry },
        { "DEBIAN", () => appSettings.Debian },
        { "MAVEN", () => appSettings.Maven },
        { "CARGO", () => appSettings.Cargo }
    };
            if (projectTypeMappings.TryGetValue(appSettings.ProjectType.ToUpperInvariant(), out var getConfig))
            {
                var config = getConfig();
                if (config != null)
                {
                    var repoList = new List<string>();
                    if (!string.IsNullOrEmpty(config.ReleaseRepo))
                    {
                        repoList.Add(config.ReleaseRepo);
                    }

                    if (!string.IsNullOrEmpty(config.DevDepRepo))
                    {
                        repoList.Add(config.DevDepRepo);
                    }

                    if (config.Artifactory != null)
                    {
                        repoList.AddRange(config.Artifactory.InternalRepos ?? Array.Empty<string>());
                        repoList.AddRange(config.Artifactory.DevRepos ?? Array.Empty<string>());
                        repoList.AddRange(config.Artifactory.RemoteRepos ?? Array.Empty<string>());
                        repoList.AddRange(config.Artifactory.ThirdPartyRepos?.Select(repo => repo.Name) ?? Array.Empty<string>());
                    }

                    return [.. repoList.Where(repo => !string.IsNullOrEmpty(repo)).Distinct()];
                }
            }

            return Array.Empty<string>();
        }
        public static string LogFolderInitialisation(CommonAppSettings appSettings, string logFileName, bool m_Verbose)
        {
            string FolderPath = DefaultLogPath;
            if (!string.IsNullOrEmpty(appSettings.Directory.LogFolder))
            {
                string defaultLogFilePath = Log4Net.CatoolLogPath;

                if (File.Exists(defaultLogFilePath))
                {
                    LoggerManager.Shutdown();
                    FolderPath = appSettings.Directory.LogFolder;
                    Log4Net.Init(logFileName, appSettings.Directory.LogFolder, m_Verbose);
                    string currentLogFilePath = Log4Net.CatoolLogPath;
                    string currentlogFileName = Path.GetFileName(Log4Net.CatoolLogPath);
                    LoggerManager.Shutdown();
                    try
                    {
                        File.Copy(defaultLogFilePath, currentLogFilePath, overwrite: true);
                    }
                    catch (IOException ioEx)
                    {
                        Logger.Debug($"IO Exception during log file copy: {ioEx.Message}");
                    }
                    catch (UnauthorizedAccessException uaEx)
                    {
                        Logger.Debug($"Unauthorized Access Exception during log file copy: {uaEx.Message}");
                    }
                    Thread.Sleep(2000);
                    Log4Net.Init(currentlogFileName, FolderPath, m_Verbose);
                }
                else
                {
                    FolderPath = appSettings.Directory.LogFolder;
                    Log4Net.Init(logFileName, appSettings.Directory.LogFolder, m_Verbose);
                }
            }
            return FolderPath;
        }
        public static void DefaultLogFolderInitialisation(string logFileName, bool m_Verbose)
        {
            string FolderPath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                FolderPath = FileConstant.LogFolder;
            }
            else
            {
                FolderPath = "/var/log";
            }
            Log4Net.Init(logFileName, FolderPath, m_Verbose);
            DefaultLogPath = FolderPath;
        }
        public static bool ContainsInvalidCharacters(string projectName, out string invalidChars)
        {
            var foundInvalidChars = projectName.Where(c => InvalidProjectNameChars.Contains(c))
                                             .Distinct()
                                             .ToList();

            invalidChars = string.Join(", ", foundInvalidChars.Select(c => $"'{c}'"));
            return foundInvalidChars.Count != 0;
        }
        public static int ValidateSw360Project(string sw360ProjectName, string clearingState, string Name, CommonAppSettings appSettings)
        {
            if (string.IsNullOrEmpty(sw360ProjectName))
            {
                throw new InvalidDataException($"Invalid Project Id - {appSettings.SW360.ProjectID}");
            }
            else if (ContainsInvalidCharacters(Name, out string invalidChars))
            {
                Logger.Error($"Invalid characters ({invalidChars}) found in SW360 project name '{Name}'. Create or rename project name without using these characters: '/', '\\', '.'");
                Logger.Debug($"ValidateAppSettings(): Project name validation failed for '{sw360ProjectName}' due to invalid characters: {invalidChars}");
                return -1;
            }
            else if (clearingState == "CLOSED")
            {
                Logger.Error($"Provided Sw360 project is not in active state. Please make sure you added the correct project details that are in an active state.");
                Logger.Debug($"ValidateSw360Project(): Sw360 project {Name} is in {clearingState} state.");
                return -1;
            }
            else
            {
                appSettings.SW360.ProjectName = sw360ProjectName;
            }
            return 0;
        }
        public static string[] MaskSensitiveArguments(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Logger.Debug("MaskSensitiveArguments(): No arguments passed to the method.");
                return Array.Empty<string>();
            }

            string[] maskedArgs = new string[args.Length];
            bool skipNext = false; // Flag to skip processing the next argument

            for (int i = 0; i < args.Length; i++)
            {
                if (skipNext)
                {
                    skipNext = false;
                    continue;
                }
                if (args[i].Equals("--SW360:Token", StringComparison.OrdinalIgnoreCase) ||
                    args[i].Equals("--Jfrog:Token", StringComparison.OrdinalIgnoreCase))
                {
                    maskedArgs[i] = args[i];
                    if (i + 1 < args.Length)
                    {
                        maskedArgs[i + 1] = "******";
                        skipNext = true;
                    }
                }
                else
                {
                    maskedArgs[i] = args[i];
                }
            }

            return maskedArgs;
        }

        public static Bom RemoveExcludedComponentsFromBom(CommonAppSettings appSettings, Bom cycloneDXBOM, Action<int> updateKpiCallback = null)
        {
            List<Component> componentForBOM = cycloneDXBOM.Components.ToList();
            List<Dependency> dependenciesForBOM = cycloneDXBOM.Dependencies?.ToList() ?? new List<Dependency>();
            int noOfExcludedComponents = 0;

            if (appSettings?.SW360?.ExcludeComponents != null)
            {
                componentForBOM = RemoveExcludedComponents(componentForBOM, appSettings.SW360?.ExcludeComponents, ref noOfExcludedComponents);
                updateKpiCallback?.Invoke(noOfExcludedComponents);
            }

            cycloneDXBOM.Components = componentForBOM;
            cycloneDXBOM.Dependencies = dependenciesForBOM;
            return cycloneDXBOM;
        }

        /// <summary>
        /// Processes components to add internal identification properties
        /// </summary>
        /// <param name="components">List of components to process</param>
        /// <param name="isInternalPredicate">Function to determine if a component is internal</param>
        /// <returns>Tuple containing (processedComponents, internalComponents)</returns>
        public static (List<Component> processedComponents, List<Component> internalComponents) ProcessInternalComponentIdentification(List<Component> components, Func<Component, bool> isInternalPredicate)
        {
            List<Component> internalComponents = new List<Component>();
            var processedComponents = new List<Component>();

            foreach (Component component in components)
            {
                var currentIterationItem = component;
                bool isTrue = isInternalPredicate(currentIterationItem);

                currentIterationItem.Properties ??= new List<Property>();

                string isInternalValue = isTrue ? "true" : "false";
                var properties = currentIterationItem.Properties;
                CommonHelper.RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_IsInternal, isInternalValue);
                currentIterationItem.Properties = properties;
                if (isTrue)
                {
                    internalComponents.Add(currentIterationItem);
                }

                processedComponents.Add(currentIterationItem);
            }

            return (processedComponents, internalComponents);
        }

        /// <summary>
        /// Sets standard component properties and hashes for JFrog components
        /// </summary>
        /// <param name="component">Component to update</param>
        /// <param name="artifactoryRepo">Artifactory repository name</param>
        /// <param name="projectType">Project type property</param>
        /// <param name="siemensFileName">Siemens filename property</param>
        /// <param name="jfrogRepoPath">JFrog repository path property</param>
        /// <param name="hashes">Optional AQL result containing hash values</param>
        public static void SetComponentPropertiesAndHashes(Component component,
     Property artifactoryRepo,
     Property projectType,
     Property siemensFileName,
     Property jfrogRepoPath,
     dynamic hashes = null)
        {
            component.Properties ??= new List<Property>();
            var properties = component.Properties;
            RemoveDuplicateAndAddProperty(ref properties, artifactoryRepo?.Name, artifactoryRepo?.Value);
            RemoveDuplicateAndAddProperty(ref properties, projectType?.Name, projectType?.Value);
            RemoveDuplicateAndAddProperty(ref properties, siemensFileName?.Name, siemensFileName?.Value);
            RemoveDuplicateAndAddProperty(ref properties, jfrogRepoPath?.Name, jfrogRepoPath?.Value);
            component.Properties = properties;
            component.Description = null;
            if (hashes != null)
            {
                component.Hashes = new List<Hash>()
        {
            new()
            {
                Alg = Hash.HashAlgorithm.MD5,
                Content = hashes.MD5
            },
            new()
            {
                Alg = Hash.HashAlgorithm.SHA_1,
                Content = hashes.SHA1
            },
            new()
            {
                Alg = Hash.HashAlgorithm.SHA_256,
                Content = hashes.SHA256
            }
        };
            }
        }
        public static void AddSpdxSBomFileNameProperty(ref Bom bom, string filePath)
        {
            if (bom?.Components != null)
            {
                string filename = Path.GetFileName(filePath);
                var bomComponentsList = bom.Components;
                foreach (var component in bomComponentsList)
                {
                    component.Properties ??= new List<Property>();
                    SpdxSbomHelper.AddSpdxComponentProperties(filename, component);
                }
                bom.Components = bomComponentsList;
            }

        }
        public static void RemoveDuplicateAndAddProperty(ref List<Property> properties, string propertyName, string propertyValue)
        {
            properties ??= new List<Property>();
            properties.RemoveAll(p => p.Name == propertyName);
            properties.Add(new Property { Name = propertyName, Value = propertyValue });
        }
        public static string Sw360URL(string sw360Env, string releaseId)
        {
            string sw360URL = $"{sw360Env}{"/group/guest/components/-/component/release/detailRelease/"}{releaseId}";
            return sw360URL;
        }
        public static string Sw360ComponentURL(string sw360Env, string componentId)
        {
            string sw360URL = $"{sw360Env}{"/group/guest/components/-/component/detail/"}{componentId}";
            return sw360URL;
        }
        #endregion

        #region private
        private static string WildcardToRegex(string wildcard)
        {
            return "^" + Regex.Escape(wildcard).Replace("\\*", ".*") + "$";
        }

        private static void AddExcludedComponentsPropertyFromPurl(List<Component> ComponentList, List<string> ExcludedComponentsFromPurl, ref int noOfExcludedComponents)
        {
            foreach (string excludedComponent in ExcludedComponentsFromPurl)
            {
                foreach (var component in ComponentList)
                {
                    string componentPurl = NormalizePurl(component.Purl);
                    if (component.Purl != null && componentPurl.Equals(excludedComponent, StringComparison.OrdinalIgnoreCase))
                    {
                        component.Properties ??= new List<Property>();
                        var properties = component.Properties;
                        RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_ExcludeComponent, "true");
                        component.Properties = properties;
                        noOfExcludedComponents++;
                    }
                }
            }
        }
        private static string NormalizePurl(string purl)
        {
            if (purl.Contains("%40"))
            {
                return purl.Replace("%40", "@");
            }
            return purl;
        }
        private static void AddExcludedComponentsPropertyFromNameAndVersion(List<Component> ComponentList, List<string> otherExcludedComponents, ref int noOfExcludedComponents)
        {
            foreach (string excludedComponent in otherExcludedComponents)
            {
                string[] excludedcomponent = excludedComponent.ToLower().Split(':');
                foreach (var component in ComponentList)
                {
                    string name = component.Name;
                    if (!string.IsNullOrEmpty(component.Group) && (component.Group != component.Name))
                    {
                        name = $"{component.Group}/{component.Name}";
                    }
                    if (excludedcomponent.Length > 0 && (Regex.IsMatch(name.ToLowerInvariant(), WildcardToRegex(excludedcomponent[0].ToLowerInvariant()))) &&
                        (component.Version.Contains(excludedcomponent[1], StringComparison.InvariantCultureIgnoreCase) || excludedcomponent[1].Equals("*", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        noOfExcludedComponents++;
                        component.Properties ??= new List<Property>();
                        var properties = component.Properties;
                        RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_ExcludeComponent, "true");
                        component.Properties = properties;
                    }
                }
            }
        }
        public static Component CreateComponentWithProperties(string name, string version, string releaseExternalId)
        {
            Component component = new Component
            {
                Name = name,
                Version = version,
                Purl = releaseExternalId,
                BomRef = releaseExternalId,
                Type = Component.Classification.Library
            };
            return component;
        }
        public static string CanonicalizeProjectType(string projectType)
        {
            if (string.IsNullOrWhiteSpace(projectType))
                return projectType;

            return projectType.Trim().ToUpperInvariant() switch
            {
                "POETRY" => "Poetry",
                "CARGO" => "Cargo",
                "CONAN" => "Conan",
                "DEBIAN" => "Debian",
                "MAVEN" => "Maven",
                "NPM" => "npm",
                "NUGET" => "NuGet",
                "ALPINE" => "Alpine",
                _ => projectType.Trim(),
            };
        }
        public static void ApplyCdxGenEnrichment(ref List<Component> ListofComponentsFromLockFile, ref List<Dependency> ListofDependenciesFromLockFile, ref List<Component> componentsForBOM, ref List<Dependency> dependencies,Bom? cdxGenBomData)
        {
            if (cdxGenBomData?.Components == null || cdxGenBomData.Components.Count == 0)
            {
                return;
            }

            EnrichComponentsFromCdxGen(ref ListofComponentsFromLockFile, cdxGenBomData.Components);
            
            if (cdxGenBomData.Components != null && cdxGenBomData.Components.Count > 0)
            {
                componentsForBOM.AddRange(cdxGenBomData.Components);
            }

            if (cdxGenBomData.Dependencies != null && cdxGenBomData.Dependencies.Count > 0)
            {
                dependencies.AddRange(cdxGenBomData.Dependencies);
            }          
            AddSiemensDirectProperty(ref cdxGenBomData);
        }
        public static void AddSiemensDirectProperty(ref Bom bom)
        {
            List<string> npmDirectDependencies = new List<string>();
            npmDirectDependencies.AddRange(bom.Dependencies?.Select(x => x.Ref).ToList() ?? new List<string>());
            var bomComponentsList = bom.Components;

            foreach (var component in bomComponentsList)
            {
                string siemensDirectValue = npmDirectDependencies.Exists(x => x.Contains(component.Name) && x.Contains(component.Version))
                    ? "true"
                    : "false";
                component.Properties ??= new List<Property>();
                var properties = component.Properties;
                CommonHelper.RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_SiemensDirect, siemensDirectValue);
                component.Properties = properties;
            }
            bom.Components = bomComponentsList;
        }
        private static void EnrichComponentsFromCdxGen(ref List<Component> componentsForBOM, List<Component> cdxComponents)
        {
           
            var byPurl = new Dictionary<string, Component>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in componentsForBOM.Where(c => !string.IsNullOrEmpty(c.Purl)))
            {
                byPurl[c.Purl] = c;
            }

            var byNameVer = new Dictionary<string, Component>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in componentsForBOM.Where(c => !string.IsNullOrEmpty(c.Name) && !string.IsNullOrEmpty(c.Version)))
            {
                var key = $"{c.Name}|{c.Version}";
                byNameVer[key] = c;
            }

            foreach (var cdx in cdxComponents)
            {
                MergeExistingPropertiesIntoCdx(cdx, byPurl, byNameVer);
            }
        }       
        private static void MergeExistingPropertiesIntoCdx(
            Component cdx,
            Dictionary<string, Component> byPurl,
            Dictionary<string, Component> byNameVer)
        {
            Component existing = null;

            if (!string.IsNullOrEmpty(cdx.Purl) && byPurl.TryGetValue(cdx.Purl, out var matchByPurl))
            {
                existing = matchByPurl;
            }
            else
            {
                var key = (!string.IsNullOrEmpty(cdx.Name) && !string.IsNullOrEmpty(cdx.Version))
                    ? $"{cdx.Name}|{cdx.Version}"
                    : null;

                if (key != null && byNameVer.TryGetValue(key, out var matchByNameVer))
                {
                    existing = matchByNameVer;
                }
            }

            if (existing != null && existing.Properties != null && existing.Properties.Count > 0)
            {
                cdx.Properties = [.. existing.Properties.Select(p => new Property { Name = p.Name, Value = p.Value })];
            }
            else
            {
                cdx.Properties ??= new List<Property>();
            }
        }        
        #endregion
    }
}