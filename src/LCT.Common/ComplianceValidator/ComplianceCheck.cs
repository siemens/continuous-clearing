// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Interface;
using LCT.Common.Model;
using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace LCT.Common.ComplianceValidator
{
    public class ComplianceCheck : IChecker
    {
        #region Fields
        /// <summary>
        /// Logger instance for compliance check logging.
        /// </summary>
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        /// <summary>
        /// Stores warning messages generated during compliance checks.
        /// </summary>
        private List<string> Warnings = new();
        #endregion

        #region Properties
        // No properties present.
        #endregion

        #region Constructors
        // No constructors present.
        #endregion

        #region Methods
        /// <summary>
        /// Asynchronously loads compliance settings from a JSON file.
        /// </summary>
        /// <param name="jsonFilePath">The full path to the JSON file containing the compliance settings. Must not be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a ComplianceSettingsModel object populated with the settings from the specified JSON file.</returns>
        public async Task<ComplianceSettingsModel> LoadSettingsAsync(string jsonFilePath)
        {
            using var stream = System.IO.File.OpenRead(jsonFilePath);
            var settings = await JsonSerializer.DeserializeAsync<ComplianceSettingsModel>(stream);
            return settings;
        }

        /// <summary>
        /// Evaluates compliance settings and data to determine if any compliance warnings or recommendations exist.
        /// </summary>
        /// <param name="settings">The compliance settings model containing exception components and compliance instructions. Cannot be null.</param>
        /// <param name="data">The data to be checked for compliance. Must be a List of ComparisonBomData objects. If the data is not of the expected type or is null, the method returns false.</param>
        /// <returns>True if no compliance warnings are detected; otherwise, false.</returns>
        public bool Check(ComplianceSettingsModel settings, object data)
        {
            Warnings = [];

            if (settings == null || data == null)
                return false;

            if (data is not List<ComparisonBomData> bomDataList)
                return false;

            var purlToComponent = BuildPurlToComponentMap(settings.ComplianceExceptionComponents);
            var groupMap = GroupBomDataByWarningAndRecommendation(bomDataList, purlToComponent);

            if (groupMap.Count > 0)
            {
                PrintWarning("*Compliance Exception occured");
            }

            bool hasWarning = false;
            foreach (var kvp in groupMap)
            {
                hasWarning |= HandleGroup(kvp.Key.warning, kvp.Key.recommendation, kvp.Value);
            }

            return hasWarning;
        }

        /// <summary>
        /// Builds a dictionary mapping PURL strings to their corresponding compliance exception components.
        /// </summary>
        /// <param name="components">The collection of compliance exception components.</param>
        /// <returns>A dictionary mapping PURL strings to compliance exception components.</returns>
        private static Dictionary<string, ComplianceExceptionComponent> BuildPurlToComponentMap(IEnumerable<ComplianceExceptionComponent> components)
        {
            var map = new Dictionary<string, ComplianceExceptionComponent>();
            foreach (var comp in components ?? Enumerable.Empty<ComplianceExceptionComponent>())
            {
                if (comp.Purl == null) continue;
                foreach (var purl in comp.Purl.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    map[purl] = comp;
                }
            }
            return map;
        }

        /// <summary>
        /// Groups BOM data by warning and recommendation messages.
        /// </summary>
        /// <param name="bomDataList">The list of BOM data to group.</param>
        /// <param name="purlToComponent">The dictionary mapping PURLs to compliance exception components.</param>
        /// <returns>A dictionary with keys as (warning, recommendation) and values as lists of affected PURLs.</returns>
        private static Dictionary<(string warning, string recommendation), List<string>> GroupBomDataByWarningAndRecommendation(
            List<ComparisonBomData> bomDataList,
            Dictionary<string, ComplianceExceptionComponent> purlToComponent)
        {
            var groupMap = new Dictionary<(string warning, string recommendation), List<string>>();
            foreach (var externalId in bomDataList.Select(bom => bom.ComponentExternalId).Where(id => id != null))
            {
                if (purlToComponent.TryGetValue(externalId, out var comp))
                {
                    var warning = comp.ComplianceInstructions?.WarningMessage?.Trim() ?? string.Empty;
                    var recommendation = comp.ComplianceInstructions?.Recommendation?.Trim() ?? string.Empty;
                    var key = (warning, recommendation);

                    if (!groupMap.TryGetValue(key, out var purlList))
                    {
                        purlList = new List<string>();
                        groupMap[key] = purlList;
                    }
                    purlList.Add(externalId);
                }
            }
            return groupMap;
        }

        /// <summary>
        /// Handles a group of warnings and recommendations, logging them and returning whether a warning was present.
        /// </summary>
        /// <param name="warning">The warning message.</param>
        /// <param name="recommendation">The recommendation message.</param>
        /// <param name="purls">The list of affected PURLs.</param>
        /// <returns>True if a warning was present; otherwise, false.</returns>
        private bool HandleGroup(string warning, string recommendation, List<string> purls)
        {
            bool hasWarning = false;
            Warnings.Add(warning + Environment.NewLine);

            if (!string.IsNullOrWhiteSpace(warning))
            {
                PrintWarning("[WARNING] " + warning);
                PrintWarning("Affected PURLs:");
                foreach (var purl in purls)
                {
                    PrintWarning("  " + purl);
                }
                hasWarning = true;
            }
            if (!string.IsNullOrWhiteSpace(recommendation))
            {
                PrintRecommendation("[RECOMMENDATION] " + recommendation);
            }
            Logger.Logger.Log(null, Level.Info, "", null);
            return hasWarning;
        }

        /// <summary>
        /// Logs the provided recommendation content as an informational message.
        /// </summary>
        /// <param name="content">The recommendation content to log. Must not be null, empty, or consist only of whitespace.</param>
        /// <returns>void.</returns>
        public void PrintRecommendation(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            Logger.Logger.Log(null, Level.Info, content, null); // Green/info
        }

        /// <summary>
        /// Logs a warning message with the specified content.
        /// </summary>
        /// <param name="content">The warning message to log. Must not be null, empty, or consist only of whitespace.</param>
        /// <returns>void.</returns>
        public void PrintWarning(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            Logger.Logger.Log(null, Level.Warn, content, null); // Orange/warn
        }

        /// <summary>
        /// Retrieves the list of warnings currently stored.
        /// </summary>
        /// <returns>A list of strings containing the warnings. The list may be empty if no warnings are present.</returns>
        public List<string> GetResults()
        {
            return Warnings;
        }
        #endregion

        #region Events
        // No events present.
        #endregion
    }
}