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
        #region Fields

        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly char[] InvalidProjectNameChars = new char[] { '/', '\\', '.' };

        #endregion

        #region Properties

        public static string ProjectSummaryLink { get; set; }
        public static string DefaultLogPath { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Checks if Azure DevOps debug mode is enabled.
        /// </summary>
        /// <returns>True if Azure DevOps debug mode is enabled; otherwise, false.</returns>
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
        /// Removes excluded components from the component list based on PURL or name/version matching.
        /// </summary>
        /// <param name="ComponentList">The list of components to filter.</param>
        /// <param name="ExcludedComponents">The list of excluded component identifiers.</param>
        /// <param name="noOfExcludedComponents">The count of excluded components.</param>
        /// <returns>The filtered component list.</returns>
        public static List<Component> RemoveExcludedComponents(List<Component> ComponentList, List<string> ExcludedComponents, ref int noOfExcludedComponents)
        {
            List<string> ExcludedComponentsFromPurl = ExcludedComponents?.Where(ec => ec.StartsWith("pkg:")).ToList();
            List<string> otherExcludedComponents = ExcludedComponents?.Where(ec => !ec.StartsWith("pkg:")).ToList();

            AddExcludedComponentsPropertyFromPurl(ComponentList, ExcludedComponentsFromPurl, ref noOfExcludedComponents);
            AddExcludedComponentsPropertyFromNameAndVersion(ComponentList, otherExcludedComponents, ref noOfExcludedComponents);
            return ComponentList;
        }

        /// <summary>
        /// Removes invalid dependencies and references that don't match existing component BOM references.
        /// </summary>
        /// <param name="components">The list of valid components.</param>
        /// <param name="dependencies">The list of dependencies to validate.</param>
        /// <returns>The cleaned list of dependencies.</returns>
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

        /// <summary>
        /// Gets the substring after the last occurrence of a separator.
        /// </summary>
        /// <param name="value">The string to search.</param>
        /// <param name="separator">The separator string.</param>
        /// <returns>The substring after the last occurrence of the separator.</returns>
        public static string GetSubstringOfLastOccurance(string value, string separator)
        {
            string result = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
            if (result.Contains(separator))
            {
                result = result[(result.LastIndexOf(separator) + separator.Length)..];
            }

            return result;
        }

        /// <summary>
        /// Trims the specified suffix from the end of the string if present.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="suffixToRemove">The suffix to remove.</param>
        /// <param name="comparisonType">The string comparison type.</param>
        /// <returns>The string with the suffix removed, or the original string.</returns>
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

        /// <summary>
        /// Checks if a string value is null or empty and throws an exception if invalid.
        /// </summary>
        /// <param name="name">The name of the parameter being checked.</param>
        /// <param name="value">The value to check.</param>
        public static void CheckNullOrEmpty(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Invalid value for {name} - {value}");
            }
        }

        /// <summary>
        /// Checks if a component has a property with the specified constant name.
        /// </summary>
        /// <param name="component">The component to check.</param>
        /// <param name="constant">The property name constant to search for.</param>
        /// <returns>True if the property exists; otherwise, false.</returns>
        public static bool ComponentPropertyCheck(Component component, string constant)
        {
            if (component.Properties == null)
            {
                return false;
            }
            return component.Properties.Exists(x => x.Name == constant);
        }

        /// <summary>
        /// Gets and sets properties for manually added components based on file type.
        /// </summary>
        /// <param name="componentsForBOM">The components to process.</param>
        /// <param name="listComponentForBOM">The output list for processed components.</param>
        /// <param name="filePath">The file path to determine component properties.</param>
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

        /// <summary>
        /// Adds specific metadata values to BOM format and serializes it.
        /// </summary>
        /// <param name="listOfComponentsToBom">The BOM to update and serialize.</param>
        /// <returns>The serialized BOM data as a JSON string.</returns>
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

        /// <summary>
        /// Gets the list of repositories configured for the project type.
        /// </summary>
        /// <param name="appSettings">The application settings containing repository configuration.</param>
        /// <returns>An array of repository names.</returns>
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
        { "CARGO", () => appSettings.Cargo },
        { "CHOCO", () => appSettings.Choco }
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

        /// <summary>
        /// Initializes the log folder based on application settings and migrates existing logs.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="logFileName">The log file name.</param>
        /// <param name="m_Verbose">Whether verbose logging is enabled.</param>
        /// <returns>The initialized log folder path.</returns>
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

        /// <summary>
        /// Initializes the default log folder based on the operating system.
        /// </summary>
        /// <param name="logFileName">The log file name.</param>
        /// <param name="m_Verbose">Whether verbose logging is enabled.</param>
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

        /// <summary>
        /// Checks if a project name contains invalid characters.
        /// </summary>
        /// <param name="projectName">The project name to validate.</param>
        /// <param name="invalidChars">The output string containing found invalid characters.</param>
        /// <returns>True if invalid characters are found; otherwise, false.</returns>
        public static bool ContainsInvalidCharacters(string projectName, out string invalidChars)
        {
            var foundInvalidChars = projectName.Where(c => InvalidProjectNameChars.Contains(c))
                                             .Distinct()
                                             .ToList();

            invalidChars = string.Join(", ", foundInvalidChars.Select(c => $"'{c}'"));
            return foundInvalidChars.Count != 0;
        }

        /// <summary>
        /// Validates the SW360 project name and state.
        /// </summary>
        /// <param name="sw360ProjectName">The SW360 project name.</param>
        /// <param name="clearingState">The clearing state of the project.</param>
        /// <param name="Name">The project name.</param>
        /// <param name="appSettings">The application settings.</param>
        /// <returns>0 if validation succeeds; -1 if validation fails.</returns>
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

        /// <summary>
        /// Masks sensitive arguments (tokens) in the argument array for logging purposes.
        /// </summary>
        /// <param name="args">The command line arguments to mask.</param>
        /// <returns>An array with sensitive values replaced by asterisks.</returns>
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

        /// <summary>
        /// Removes excluded components from the BOM based on application settings.
        /// </summary>
        /// <param name="appSettings">The application settings containing exclusion rules.</param>
        /// <param name="cycloneDXBOM">The BOM to process.</param>
        /// <param name="updateKpiCallback">Optional callback to update KPI with exclusion count.</param>
        /// <returns>The BOM with excluded components removed.</returns>
        public static Bom RemoveExcludedComponentsFromBom(CommonAppSettings appSettings, Bom cycloneDXBOM, Action<int> updateKpiCallback = null)
        {
            List<Component> componentForBOM = cycloneDXBOM.Components.ToList();
            List<Dependency> dependenciesForBOM = cycloneDXBOM.Dependencies?.ToList() ?? new List<Dependency>();
            int noOfExcludedComponents = 0;

            if (appSettings?.SW360?.ExcludeComponents != null)
            {
                componentForBOM = RemoveExcludedComponents(componentForBOM, appSettings.SW360?.ExcludeComponents, ref noOfExcludedComponents);
                dependenciesForBOM = RemoveInvalidDependenciesAndReferences(componentForBOM, dependenciesForBOM);
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

        /// <summary>
        /// Adds SPDX SBOM file name property to all components in the BOM.
        /// </summary>
        /// <param name="bom">The BOM to update.</param>
        /// <param name="filePath">The file path of the SPDX file.</param>
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

        /// <summary>
        /// Removes duplicate properties with the same name and adds a new property.
        /// </summary>
        /// <param name="properties">The property list to update.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The property value.</param>
        public static void RemoveDuplicateAndAddProperty(ref List<Property> properties, string propertyName, string propertyValue)
        {
            properties ??= new List<Property>();
            properties.RemoveAll(p => p.Name == propertyName);
            properties.Add(new Property { Name = propertyName, Value = propertyValue });
        }

        /// <summary>
        /// Constructs the SW360 release URL.
        /// </summary>
        /// <param name="sw360Env">The SW360 environment URL.</param>
        /// <param name="releaseId">The release ID.</param>
        /// <returns>The complete SW360 release URL.</returns>
        public static string Sw360URL(string sw360Env, string releaseId)
        {
            string sw360URL = $"{sw360Env}{"/group/guest/components/-/component/release/detailRelease/"}{releaseId}";
            return sw360URL;
        }

        /// <summary>
        /// Constructs the SW360 component URL.
        /// </summary>
        /// <param name="sw360Env">The SW360 environment URL.</param>
        /// <param name="componentId">The component ID.</param>
        /// <returns>The complete SW360 component URL.</returns>
        public static string Sw360ComponentURL(string sw360Env, string componentId)
        {
            string sw360URL = $"{sw360Env}{"/group/guest/components/-/component/detail/"}{componentId}";
            return sw360URL;
        }

        /// <summary>
        /// Converts a wildcard pattern to a regular expression pattern.
        /// </summary>
        /// <param name="wildcard">The wildcard pattern.</param>
        /// <returns>A regex pattern string.</returns>
        private static string WildcardToRegex(string wildcard)
        {
            return "^" + Regex.Escape(wildcard).Replace("\\*", ".*") + "$";
        }

        /// <summary>
        /// Adds excluded component property for components matching PURL patterns.
        /// </summary>
        /// <param name="ComponentList">The list of components to check.</param>
        /// <param name="ExcludedComponentsFromPurl">The list of excluded PURL patterns.</param>
        /// <param name="noOfExcludedComponents">The count of excluded components.</param>
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

        /// <summary>
        /// Normalizes a PURL by replacing encoded characters.
        /// </summary>
        /// <param name="purl">The PURL to normalize.</param>
        /// <returns>The normalized PURL.</returns>
        private static string NormalizePurl(string purl)
        {
            if (purl.Contains("%40"))
            {
                return purl.Replace("%40", "@");
            }
            return purl;
        }

        /// <summary>
        /// Adds excluded component property for components matching name and version patterns.
        /// </summary>
        /// <param name="ComponentList">The list of components to check.</param>
        /// <param name="otherExcludedComponents">The list of excluded component name:version patterns.</param>
        /// <param name="noOfExcludedComponents">The count of excluded components.</param>
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

        /// <summary>
        /// Creates a new component with specified properties.
        /// </summary>
        /// <param name="name">The component name.</param>
        /// <param name="version">The component version.</param>
        /// <param name="releaseExternalId">The release external ID (PURL).</param>
        /// <returns>A new Component object.</returns>
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

        /// <summary>
        /// Canonicalizes the project type to the standard format.
        /// </summary>
        /// <param name="projectType">The project type to canonicalize.</param>
        /// <returns>The canonicalized project type string.</returns>
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

        #endregion
    }
}