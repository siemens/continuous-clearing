// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
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
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LCT.Common
{
    /// <summary>
    /// Common Helper class
    /// </summary>
    public static class CommonHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static string ProjectSummaryLink { get; set; }

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
            foreach (string excludedComponent in ExcludedComponents)
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
                        (component.Version.ToLowerInvariant().Contains(excludedcomponent[1].ToLowerInvariant())|| excludedcomponent[1].ToLowerInvariant() == "*"))
                    {
                        noOfExcludedComponents++;
                        ExcludedList.Add(component);
                    }
                }
            }
            ComponentList.RemoveAll(item => ExcludedList.Contains(item));
            return ComponentList;
        }

        public static string GetSubstringOfLastOccurance(string value, string separator)
        {
            string result = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
            if (result.Contains(separator))
            {
                result = result?[(result.LastIndexOf(separator) + separator.Length)..];
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
            Logger.Info($"{"|",5}{Feature,70} {"|",5} {Count,5} {"|",5}");
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
                Environment.ExitCode = 2;
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
                Environment.ExitCode = 2;
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

        public static void GetDetailsforManuallyAdded(List<Component> componentsForBOM, List<Component> listComponentForBOM)
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
        #endregion
    }
}




