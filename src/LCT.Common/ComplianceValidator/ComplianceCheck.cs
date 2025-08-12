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
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public List<string> Warnings => [];

        /// <summary>
        /// Asynchronously loads compliance settings from a JSON file.
        /// </summary>
        /// <remarks>This method reads the specified JSON file and deserializes its content into a <see
        /// cref="ComplianceSettingsModel"/> object. Ensure the file exists and contains valid JSON data that matches
        /// the structure of <see cref="ComplianceSettingsModel"/>.</remarks>
        /// <param name="jsonFilePath">The full path to the JSON file containing the compliance settings. Must not be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see
        /// cref="ComplianceSettingsModel"/> object populated with the settings from the specified JSON file.</returns>
        public async Task<ComplianceSettingsModel> LoadSettingsAsync(string jsonFilePath)
        {
            using var stream = System.IO.File.OpenRead(jsonFilePath);
            var settings = await JsonSerializer.DeserializeAsync<ComplianceSettingsModel>(stream);
            return settings;
        }

        /// <summary>
        /// Evaluates compliance settings and data to determine if any compliance warnings or recommendations exist.
        /// </summary>
        /// <remarks>This method processes the provided compliance settings and data to identify any
        /// compliance exceptions. If compliance warnings are found, they are logged and displayed, along with any
        /// associated recommendations. The method returns <see langword="false"/> if warnings are present or if the
        /// input data is invalid.</remarks>
        /// <param name="settings">The compliance settings model containing exception components and compliance instructions. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="data">The data to be checked for compliance. Must be a <see cref="List{T}"/> of <see cref="ComparisonBomData"/>
        /// objects. If the data is not of the expected type or is <see langword="null"/>, the method returns <see
        /// langword="false"/>.</param>
        /// <returns><see langword="true"/> if no compliance warnings are detected; otherwise, <see langword="false"/>.</returns>
        public bool Check(ComplianceSettingsModel settings, object data)
        {
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

        private static Dictionary<(string warning, string recommendation), List<string>> GroupBomDataByWarningAndRecommendation(
            List<ComparisonBomData> bomDataList,
            Dictionary<string, ComplianceExceptionComponent> purlToComponent)
        {
            var groupMap = new Dictionary<(string warning, string recommendation), List<string>>();
            foreach (var bom in bomDataList)
            {
                if (bom.ComponentExternalId != null && purlToComponent.TryGetValue(bom.ComponentExternalId, out var comp))
                {
                    var warning = comp.ComplianceInstructions?.WarningMessage?.Trim() ?? string.Empty;
                    var recommendation = comp.ComplianceInstructions?.Recommendation?.Trim() ?? string.Empty;
                    var key = (warning, recommendation);

                    if (!groupMap.TryGetValue(key, out var purlList))
                    {
                        purlList = new List<string>();
                        groupMap[key] = purlList;
                    }
                    purlList.Add(bom.ComponentExternalId);
                }
            }
            return groupMap;
        }

        private bool HandleGroup(string warning, string recommendation, List<string> purls)
        {
            bool hasWarning = false;
            Warnings.Add(warning + Environment.NewLine);

            if (!string.IsNullOrWhiteSpace(warning))
            {
                PrintWarning($"[WARNING] {warning}");
                PrintWarning("Affected PURLs:");
                foreach (var purl in purls)
                {
                    PrintWarning($"  {purl}");
                }
                hasWarning = true;
            }
            if (!string.IsNullOrWhiteSpace(recommendation))
            {
                PrintRecommendation($"[RECOMMENDATION] {recommendation}");
            }
            Logger.Logger.Log(null, Level.Info, $"", null);
            return hasWarning;
        }

        /// <summary>
        /// Logs the provided recommendation content as an informational message.
        /// </summary>
        /// <remarks>If the <paramref name="content"/> is null, empty, or contains only whitespace, the
        /// method does nothing.</remarks>
        /// <param name="content">The recommendation content to log. Must not be null, empty, or consist only of whitespace.</param>
        public void PrintRecommendation(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            Logger.Logger.Log(null, Level.Info, $"{content}", null); // Green/info
        }

        /// <summary>
        /// Logs a warning message with the specified content.
        /// </summary>
        /// <remarks>If the <paramref name="content"/> is null, empty, or contains only whitespace, the
        /// method does nothing.</remarks>
        /// <param name="content">The warning message to log. Must not be null, empty, or consist only of whitespace.</param>
        public void PrintWarning(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            Logger.Logger.Log(null, Level.Warn, $"{content}", null); // Orange/warn
        }

        /// <summary>
        /// Retrieves the list of warnings currently stored.
        /// </summary>
        /// <returns>A list of strings containing the warnings. The list may be empty if no warnings are present.</returns>
        public List<string> GetResults()
        {
            return Warnings;
        }
    }
}