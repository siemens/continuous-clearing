﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

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
using Level = log4net.Core.Level;

namespace LCT.Common
{
    /// <summary>
    /// Common Helper class
    /// </summary>
    public static class CommonHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly char[] InvalidProjectNameChars = new char[] { '/', '\\', '.' };
        public static string ProjectSummaryLink { get; set; }
        public static string DefaultLogPath { get; set; }

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

        public static List<Component> RemoveExcludedComponents(List<Component> ComponentList, List<string> ExcludedComponents, ref int noOfExcludedComponents)
        {
            List<Component> ExcludedList = new List<Component>();
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

        public static void WriteToConsoleTable(Dictionary<string, int> printData, Dictionary<string, double> printTimingData)
        {
            const string Count = "Count";
            const string Feature = "Feature";
            const string TimeTakenBy = "Time Taken By";
            Logger.Info("\n");
            Logger.Info("Summary :\n");
            if (!string.IsNullOrWhiteSpace(ProjectSummaryLink)) { Logger.Info($"{ProjectSummaryLink}"); }
            Logger.Info($"{"=",5}{string.Join("", Enumerable.Repeat("=", 88)),5}");
            Logger.Info($"{"|",5}{Feature,-70} {"|",5} {Count,5} {"|",5}");
            Logger.Info($"{"=",5}{string.Join("", Enumerable.Repeat("=", 88)),5}");
            foreach (var item in printData)
            {
                if (item.Key == "Packages Not Uploaded Due To Error" || item.Key == "Packages Not Existing in Remote Cache")
                {
                    if (item.Value > 0)
                    {
                        Logger.Error($"{"|",5}{item.Key,-70} {"|",5} {item.Value,5} {"|",5}");
                        Logger.Error($"{"-",5}{string.Join("", Enumerable.Repeat("-", 88)),5}");
                    }
                    else
                    {
                        Logger.Info($"{"|",5}{item.Key,-70} {"|",5} {item.Value,5} {"|",5}");
                        Logger.Info($"{"-",5}{string.Join("", Enumerable.Repeat("-", 88)),5}");
                    }
                }
                else
                {

                    Logger.Info($"{"|",5}{item.Key,-70} {"|",5} {item.Value,5} {"|",5}");
                    Logger.Info($"{"-",5}{string.Join("", Enumerable.Repeat("-", 88)),5}");
                }

            }

            foreach (var item in printTimingData)
            {
                Logger.Info($"\n{TimeTakenBy,8} {item.Key,-5} {":",1} {item.Value,8} s\n");
            }
        }

        public static void WriteComponentsWithoutDownloadURLToKpi(List<ComparisonBomData> componentInfo, List<Components> lstReleaseNotCreated, string sw360URL)
        {
            const string Name = "Name";
            const string Version = "Version";
            const string URL = "SW360 Release URL";
            if (componentInfo.Count > 0 || lstReleaseNotCreated.Count > 0)
            {
                Logger.Logger.Log(null, Level.Alert, "Action Item required by the user:\n", null);
                EnvironmentHelper environmentHelper = new EnvironmentHelper();
                environmentHelper.CallEnvironmentExit(2);
            }

            if (componentInfo.Count > 0)
            {
                Logger.Logger.Log(null, Level.Alert, "* List of components without source download URL :", null);
                Logger.Logger.Log(null, Level.Alert, " Update the source download URL & Upload the source code manually if the SRC attachment is missing for the component", null);

                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 206)),5}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"|",5}{Name,-45} {"|",5} {Version,25} {"|",5}  {URL,-120}  {"|",-4}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 206)),5}", null);

                foreach (var item in componentInfo)
                {
                    string Link = Sw360URL(sw360URL, item.ReleaseID);
                    Logger.Logger.Log(null, Level.Alert, $"{"|",5}{item.Name,-45} {"|",5} {item.Version,25} {"|",5} {Link,-120} {"|",-5}", null);
                    Logger.Logger.Log(null, Level.Alert, $"{"-",5}{string.Join("", Enumerable.Repeat("-", 206)),5}", null);
                }

                Logger.Info("\n");
            }

            if (lstReleaseNotCreated.Count > 0)
            {
                Logger.Logger.Log(null, Level.Alert, "* List of components or releases not created in SW360 :", null);
                Logger.Logger.Log(null, Level.Alert, "  There could be network/SW360/FOSSology server problem. Check and Re-Run the pipeline.Check the logs for more details", null);
                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 86)),5}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"|",5}{Name,45} {"|",5} {Version,25} {"|",8}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 86)),5}", null);

                foreach (var item in lstReleaseNotCreated)
                {
                    Logger.Logger.Log(null, Level.Alert, $"{"|",5}{item.Name,-45} {"|",5} {item.Version,25} {"|",8}", null);
                    Logger.Logger.Log(null, Level.Alert, $"{"-",5}{string.Join("", Enumerable.Repeat("-", 86)),5}", null);
                }
                Logger.Info("\n");
            }
        }

        public static void WriteComponentsNotLinkedListInConsole(List<Components> components)
        {
            const string Name = "Name";
            const string Version = "Version";

            if (components.Count > 0)
            {
                EnvironmentHelper environmentHelper = new EnvironmentHelper();
                environmentHelper.CallEnvironmentExit(2);
                Logger.Logger.Log(null, Level.Alert, "* Components Not linked to project :", null);
                Logger.Logger.Log(null, Level.Alert, " Can be linked manually OR Check the Logs AND RE-Run", null);
                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 98)),5}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"|",5}{Name,-45} {"|",5} {Version,35} {"|",10}", null);
                Logger.Logger.Log(null, Level.Alert, $"{"=",5}{string.Join("", Enumerable.Repeat("=", 98)),5}", null);

                foreach (var item in components)
                {
                    Logger.Logger.Log(null, Level.Alert, $"{"|",5}{item.Name,-45} {"|",5} {item.Version,35} {"|",10}", null);
                    Logger.Logger.Log(null, Level.Alert, $"{"-",5}{string.Join("", Enumerable.Repeat("-", 98)),5}", null);
                }
                Logger.Info("\n");
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

        public static void GetDetailsForManuallyAdded(List<Component> componentsForBOM, List<Component> listComponentForBOM)
        {
            foreach (var component in componentsForBOM)
            {
                component.Properties = new List<Property>();
                Property isDev = new() { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" };
                Property identifierType = new() { Name = Dataconstant.Cdx_IdentifierType, Value = Dataconstant.ManullayAdded };
                component.Properties.Add(isDev);
                component.Properties.Add(identifierType);
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
            var projectTypeMappings = new Dictionary<string, Func<Artifactory>>
        {
        { "CONAN", () => appSettings.Conan?.Artifactory },
        { "NPM", () => appSettings.Npm?.Artifactory },
        { "NUGET", () => appSettings.Nuget?.Artifactory },
        { "POETRY", () => appSettings.Poetry?.Artifactory },
        { "DEBIAN", () => appSettings.Debian?.Artifactory },
        { "MAVEN", () => appSettings.Maven?.Artifactory }
        };

            if (projectTypeMappings.TryGetValue(appSettings.ProjectType.ToUpperInvariant(), out var getArtifactory))
            {
                var artifactory = getArtifactory();
                if (artifactory != null)
                {
                    return (artifactory.InternalRepos ?? Array.Empty<string>())
                        .Concat(artifactory.DevRepos ?? Array.Empty<string>())
                        .Concat(artifactory.RemoteRepos ?? Array.Empty<string>())
                        .Concat(artifactory.ThirdPartyRepos?.Select(repo => repo.Name) ?? Array.Empty<string>())
                        .ToArray();
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
        #endregion

        #region private
        private static string WildcardToRegex(string wildcard)
        {
            return "^" + Regex.Escape(wildcard).Replace("\\*", ".*") + "$";
        }

        private static string Sw360URL(string sw360Env, string releaseId)
        {
            string sw360URL = $"{sw360Env}{"/group/guest/components/-/component/release/detailRelease/"}{releaseId}";
            return sw360URL;
        }

        private static List<Component> AddExcludedComponentsPropertyFromPurl(List<Component> ComponentList, List<string> ExcludedComponentsFromPurl, ref int noOfExcludedComponents)
        {


            foreach (string excludedComponent in ExcludedComponentsFromPurl)
            {
                Property excludeProperty = new() { Name = Dataconstant.Cdx_ExcludeComponent, Value = "true" };
                foreach (var component in ComponentList)
                {
                    string componentPurl = NormalizePurl(component.Purl);
                    if (component.Purl != null && componentPurl.Equals(excludedComponent, StringComparison.OrdinalIgnoreCase))
                    {
                        component.Properties.Add(excludeProperty);
                        noOfExcludedComponents++;
                    }
                }
            }
            return ComponentList;
        }
        private static string NormalizePurl(string purl)
        {
            if (purl.Contains("%40"))
            {
                return purl.Replace("%40", "@");
            }
            return purl;
        }
        private static List<Component> AddExcludedComponentsPropertyFromNameAndVersion(List<Component> ComponentList, List<string> otherExcludedComponents, ref int noOfExcludedComponents)
        {

            Property excludeProperty = new() { Name = Dataconstant.Cdx_ExcludeComponent, Value = "true" };
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
                        (component.Version.ToLowerInvariant().Contains(excludedcomponent[1].ToLowerInvariant()) || excludedcomponent[1].ToLowerInvariant() == "*"))
                    {
                        noOfExcludedComponents++;
                        component.Properties.Add(excludeProperty);
                    }
                }
            }

            return ComponentList;
        }
        #endregion
    }
}




