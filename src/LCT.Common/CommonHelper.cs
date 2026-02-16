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
using File = System.IO.File;

namespace LCT.Common
{
    /// <summary>
    /// Common Helper class
    /// </summary>
    public static class CommonHelper
    {
        #region Fields

        /// <summary>
        /// The logger instance for logging messages and errors.
        /// </summary>
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Array of invalid characters for project names.
        /// </summary>
        private static readonly char[] InvalidProjectNameChars = new char[] { '/', '\\', '.' };
               

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets or sets the project summary link.
        /// </summary>
        public static string ProjectSummaryLink { get; set; }

        /// <summary>
        /// Gets or sets the default log path.
        /// </summary>
        public static string DefaultLogPath { get; set; }
        public static bool DependencyFileNotFound { get; set; } = true;
        #endregion Properties

        #region Methods

        /// <summary>
        /// Determines whether Azure DevOps debug mode is enabled.
        /// </summary>
        /// <returns>True if debug mode is enabled; otherwise, false.</returns>

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
                DependencyFileNotFound = true;
                Logger.Warn("    Cdxgen SBOM not provided for accurate dependencies");
            }            
        }

        /// Removes excluded components from the component list.
        /// </summary>
        /// <param name="ComponentList">The list of components to process.</param>
        /// <param name="ExcludedComponents">The list of components to exclude.</param>
        /// <param name="noOfExcludedComponents">The count of excluded components.</param>
        /// <returns>The filtered list of components.</returns>
        public static List<Component> RemoveExcludedComponents(List<Component> ComponentList, List<string> ExcludedComponents, ref int noOfExcludedComponents)
        {
            List<string> ExcludedComponentsFromPurl = ExcludedComponents?.Where(ec => ec.StartsWith("pkg:")).ToList();
            List<string> otherExcludedComponents = ExcludedComponents?.Where(ec => !ec.StartsWith("pkg:")).ToList();

            AddExcludedComponentsPropertyFromPurl(ComponentList, ExcludedComponentsFromPurl, ref noOfExcludedComponents);
            AddExcludedComponentsPropertyFromNameAndVersion(ComponentList, otherExcludedComponents, ref noOfExcludedComponents);
            return ComponentList;
        }

        /// <summary>
        /// Removes invalid dependencies and references that don't match component BOM references.
        /// </summary>
        /// <param name="components">The list of components.</param>
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
        /// <param name="value">The input string.</param>
        /// <param name="separator">The separator to search for.</param>
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
        /// Trims the specified suffix from the end of the input string if present.
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
        /// Gets the display name for the given instance type and property name.
        /// </summary>
        /// <param name="objectValue">The object instance.</param>
        /// <param name="nameOfProperty">The property name.</param>
        /// <returns>The display name attribute value, or empty string if not found.</returns>
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
        /// Checks if a string value is null, empty, or whitespace and logs an error if so.
        /// </summary>
        /// <param name="name">The name of the parameter being checked.</param>
        /// <param name="value">The value to check.</param>
        public static void CheckNullOrEmpty(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Logger.ErrorFormat("The provided value for '{0}' is null, empty, or whitespace. Value: '{1}'", name, value);
                LogHandlingHelper.ExceptionErrorHandling("CheckNullOrEmpty()", $"Validation failed for parameter: {name}", new ArgumentException($"Invalid value for {name} - {value}"), $"The provided value for '{name}' is null, empty, or whitespace.");
            }
        }

        /// <summary>
        /// Checks if a component has a property with the specified name.
        /// </summary>
        /// <param name="component">The component to check.</param>
        /// <param name="constant">The property name to look for.</param>
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
        /// Adds details for manually added components to the BOM list.
        /// </summary>
        /// <param name="componentsForBOM">The components to process.</param>
        /// <param name="listComponentForBOM">The target list to add processed components to.</param>
        /// <param name="filePath">The file path to extract properties from.</param>
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
        /// Adds specific values to BOM format and serializes it to JSON.
        /// </summary>
        /// <param name="listOfComponentsToBom">The BOM to format and serialize.</param>
        /// <returns>The serialized BOM JSON string.</returns>
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
        /// Gets the list of repositories based on the project type configuration.
        /// </summary>
        /// <param name="appSettings">The application settings containing repository configurations.</param>
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
        /// Initializes the log folder based on application settings.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="logFileName">The log file name.</param>
        /// <param name="m_Verbose">Whether verbose logging is enabled.</param>
        /// <returns>The log folder path.</returns>
        public static string LogFolderInitialization(CommonAppSettings appSettings, string logFileName, bool m_Verbose)
        {
            string FolderPath;
            string defaultLogFilePath = Log4Net.CatoolLogPath;
            LoggerManager.Shutdown();
            if (!string.IsNullOrEmpty(appSettings.Directory.LogFolder))
            {
                FolderPath = appSettings.Directory.LogFolder;
                Log4Net.Init(logFileName, appSettings.Directory.LogFolder, m_Verbose);
            }
            else
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    FolderPath = FileConstant.LogFolder;
                }
                else
                {
                    FolderPath = "/var/log";
                }

                Log4Net.Init(logFileName, FolderPath, m_Verbose);
            }
            CopyInitialLogsToCurrentLoggerAndDelete(defaultLogFilePath);
            return FolderPath;
        }

        /// <summary>
        /// Copies initial logs to the current logger and deletes the original log file.
        /// </summary>
        /// <param name="defaultLogFilePath">The path to the default log file.</param>
        private static void CopyInitialLogsToCurrentLoggerAndDelete(string defaultLogFilePath)
        {
            try
            {
                Logger.Debug("====================<<<<< Start >>>>>====================");
                if (!string.IsNullOrEmpty(defaultLogFilePath) && File.Exists(defaultLogFilePath))
                {
                    foreach (var line in File.ReadLines(defaultLogFilePath).Where(l => !string.IsNullOrWhiteSpace(l)))
                    {
                        var messageOnly = TrimLogHeader(line);
                        Logger.Debug(messageOnly);
                    }
                    File.Delete(defaultLogFilePath);
                }
            }
            catch (IOException ioEx)
            {
                Logger.Debug("IO Exception while Copying initial logs.", ioEx);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Logger.Debug("Unauthorized Access while Copying initial logs.", uaEx);
            }
        }

        /// <summary>
        /// Trims the log header from a log line, returning only the message content.
        /// </summary>
        /// <param name="line">The log line to trim.</param>
        /// <returns>The message content after the header.</returns>
        private static string TrimLogHeader(string line)
        {
            int first = line.IndexOf('|');
            if (first < 0) return line;

            int second = line.IndexOf('|', first + 1);
            if (second < 0) return line;

            int third = line.IndexOf('|', second + 1);
            if (third < 0) return line;

            // Return content after the third pipe, trimming leading spaces
            return line[(third + 1)..].TrimStart();
        }

        /// <summary>
        /// Initializes the default log folder in the temp directory.
        /// </summary>
        /// <param name="logFileName">The log file name.</param>
        /// <param name="m_Verbose">Whether verbose logging is enabled.</param>
        /// <returns>The local path for the source repository downloads.</returns>
        public static string DefaultLogFolderInitialization(string logFileName, bool m_Verbose)
        {
            string localPathforSourceRepo = string.Empty;
            try
            {
                localPathforSourceRepo = $"{Path.GetTempPath()}ClearingTool\\DownloadedFiles/";
                if (!System.IO.Directory.Exists(localPathforSourceRepo))
                {
                    System.IO.Directory.CreateDirectory(localPathforSourceRepo);
                }

                Log4Net.Init(logFileName, localPathforSourceRepo, m_Verbose);
                DefaultLogPath = localPathforSourceRepo;
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling(
                    "DefaultLogFolderInitialization",
                    "MethodName:DefaultLogFolderInitialization()",
                    ex,
                    "An I/O error occurred while trying to create or access the temp directory.");
                Logger.Error("DefaultLogFolderInitialization() ", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling(
                    "DefaultLogFolderInitialization",
                    "MethodName:DefaultLogFolderInitialization()",
                    ex,
                    "Unauthorized access occurred while trying to create or access the temp directory.");
                Logger.Error("DefaultLogFolderInitialization() ", ex);
            }
            return localPathforSourceRepo;
        }

        /// <summary>
        /// Checks if a project name contains invalid characters.
        /// </summary>
        /// <param name="projectName">The project name to validate.</param>
        /// <param name="invalidChars">Output parameter containing the invalid characters found.</param>
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
        /// <param name="Name">The project name to validate.</param>
        /// <param name="appSettings">The application settings.</param>
        /// <returns>0 if valid; -1 if invalid.</returns>
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
        /// Masks sensitive arguments like tokens in the argument array.
        /// </summary>
        /// <param name="args">The array of arguments to mask.</param>
        /// <returns>A new array with sensitive values masked.</returns>
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
        /// Adds SPDX SBOM filename properties to all components in the BOM.
        /// </summary>
        /// <param name="bom">The BOM to update.</param>
        /// <param name="filePath">The SPDX file path.</param>
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
        /// Removes duplicate properties by name and adds a new property.
        /// </summary>
        /// <param name="properties">The list of properties to update.</param>
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
        /// <returns>The equivalent regex pattern.</returns>
        private static string WildcardToRegex(string wildcard)
        {
            return "^" + Regex.Escape(wildcard).Replace("\\*", ".*") + "$";
        }

        /// <summary>
        /// Adds excluded component property based on PURL matching.
        /// </summary>
        /// <param name="ComponentList">The list of components.</param>
        /// <param name="ExcludedComponentsFromPurl">The list of PURLs to exclude.</param>
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
                        Logger.DebugFormat("Component excluded due to PURL match: Name = {0}, Version = {1}, PURL = {2}", component.Name, component.Version, component.Purl);
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
        /// Adds excluded component property based on name and version matching.
        /// </summary>
        /// <param name="ComponentList">The list of components.</param>
        /// <param name="otherExcludedComponents">The list of excluded components by name:version.</param>
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
                        Logger.DebugFormat("Component excluded due to Name and Version match: Name = {0}, Version = {1}", component.Name, component.Version);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a component with standard properties.
        /// </summary>
        /// <param name="name">The component name.</param>
        /// <param name="version">The component version.</param>
        /// <param name="releaseExternalId">The release external ID (PURL).</param>
        /// <returns>A new Component instance.</returns>
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
        /// Canonicalizes the project type string to standardized format.
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
        /// <summary>
        /// Merges cdxgen-generated SBOM data into existing BOM collections and enriches component properties.
        /// </summary>
        /// <param name="ListofComponentsFromLockFile">
        /// Reference to the list of components discovered from lock/package files. Used to merge existing properties into cdxgen components.
        /// </param>
        /// <param name="ListofDependenciesFromLockFile">
        /// Reference to the list of dependencies discovered from lock/package files. Currently not modified in this method.
        /// </param>
        /// <param name="componentsForBOM">
        /// Reference to the BOM component list that will be appended with components from the cdxgen SBOM.
        /// </param>
        /// <param name="dependencies">
        /// Reference to the BOM dependency list that will be appended with dependencies from the cdxgen SBOM.
        /// </param>
        /// <param name="cdxGenBomData">
        /// The parsed CycloneDX <see cref="Bom"/> produced by cdxgen. If it contains components, they are merged and appended to the BOM lists.
        /// </param>
        /// <remarks>
        /// Operation order:
        /// 1) If cdxgen components exist, enrich them with properties from lock-file components (match by PURL or Name+Version).
        /// 2) Append cdxgen components and dependencies to the target BOM lists.
        /// 3) Add the "siemens:direct" property to components based on top-level dependency refs from cdxgen.
        /// If cdxgen has no components, this method returns without changes.
        /// </remarks>

      
        public static void ApplyCdxGenEnrichment(
     ref List<Component> ListofComponentsFromLockFile,
     ref List<Dependency> ListofDependenciesFromLockFile,
     ref List<Component> componentsForBOM,
     ref List<Dependency> dependencies,
     Bom cdxGenBomData)
        {
            if (cdxGenBomData?.Components == null || cdxGenBomData.Components.Count == 0)
            {
                return;
            }

            // Step 1: Identify valid BomRefs from components discovered in lock/package files
            var validBomRefs = new HashSet<string>(
                (ListofComponentsFromLockFile ?? new List<Component>())
                    .Where(c => !string.IsNullOrEmpty(c.BomRef))
                    .Select(c => c.BomRef),
                StringComparer.OrdinalIgnoreCase);

            // Keep existing append behavior for components to BOM
            if (ListofComponentsFromLockFile != null && ListofComponentsFromLockFile.Count > 0)
            {
                componentsForBOM.AddRange(ListofComponentsFromLockFile);
            }

            // Step 2: Merge cdxgen dependencies into discovered dependencies and prune invalid refs (top-level and nested)
            if (cdxGenBomData.Dependencies != null && cdxGenBomData.Dependencies.Count > 0)
            {
                
                // Prune invalid dependencies by validBomRefs
                void Prune(List<Dependency> deps)
                {
                    if (deps == null) return;

                    // Remove any dependency whose Ref is null/empty or not in validBomRefs
                    deps.RemoveAll(d => string.IsNullOrEmpty(d.Ref) || !validBomRefs.Contains(d.Ref));

                    // Recurse into children
                    foreach (var d in deps)
                    {
                        if (d.Dependencies != null && d.Dependencies.Count > 0)
                        {
                            Prune(d.Dependencies);
                        }
                    }
                }

                Prune(cdxGenBomData.Dependencies);

                // Also reflect the pruned dependencies in the BOM dependencies collection
                dependencies.AddRange(cdxGenBomData.Dependencies);
            }
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
        /// <summary>
        /// Enriches cdxgen components by merging existing properties from already discovered BOM components.
        /// </summary>
        /// <param name="componentsForBOM">
        /// Reference to the list of components already discovered (e.g., from lock/package files).
        /// Properties from these components will be used to enrich the incoming cdxgen components.
        /// </param>
        /// <param name="cdxComponents">
        /// Components parsed from the cdxgen-generated CycloneDX BOM that need to be enriched.
        /// </param>
        /// <remarks>
        /// Matching strategy:
        /// - First attempts match by PURL (case-insensitive).
        /// - Falls back to Name+Version key when PURL is unavailable.
        /// For matched components, existing properties are cloned into the cdxgen component.
        /// </remarks>
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

        /// <summary>
        /// Merges existing component properties into a cdxgen component by matching against already discovered BOM components.
        /// </summary>
        /// <param name="cdx">
        /// The cdxgen component to enrich with properties cloned from a matching existing component.
        /// </param>
        /// <param name="byPurl">
        /// Lookup dictionary of existing components keyed by PURL (case-insensitive).
        /// </param>
        /// <param name="byNameVer">
        /// Lookup dictionary of existing components keyed by "Name|Version" (case-insensitive) for cases where PURL is unavailable.
        /// </param>
        /// <remarks>
        /// Matching strategy:
        /// - Prefer matching by PURL when available.
        /// - Fallback to matching by Name and Version.
        /// If a match is found and the existing component has properties, those properties are cloned into the target <paramref name="cdx"/>.
        /// Otherwise, ensures <paramref name="cdx"/> has a non-null properties list.
        /// </remarks>
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