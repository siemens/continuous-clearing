// Ignore Spelling: Spdx Bom LCT

using CycloneDX.Models;
using LCT.Common.Constants;
using LCT.Common.Interface;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;

namespace LCT.Common
{
    public class SpdxBomParser : ISpdxBomParser
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public Bom ParseSPDXBom(string filePath)
        {           

            var bom = InitializeBom();
            try
            {
                string json = File.ReadAllText(filePath);
                var root = JsonNode.Parse(json);

                var graph = root?["@graph"]?.AsArray();
                if (graph == null)
                {
                    Logger.Debug("Invalid SPDX file structure: '@graph' node is missing or invalid.");
                    return bom;
                }

                var spdxDoc = GetSpdxDocument(graph);
                if (spdxDoc == null)
                {
                    Logger.Debug("SPDX Document not found in the provided file.");
                    return bom;
                }

                if (!IsValidSpecVersion(graph, spdxDoc))
                {
                    Logger.Warn("Unsupported SPDX version identified.");
                    return bom;
                }

                var packageMap = BuildPackageMap(graph);
                BuildComponents(graph, bom);
                BuildDependencies(graph, packageMap, bom);
            }
            catch (System.Text.Json.JsonException ex)
            {
                Logger.Error("Failed to parse the SPDX JSON file.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error("Exception in reading spdx bom", ex);
            }
            catch (FileNotFoundException ex)
            {
                Logger.Error("Exception in reading spdx bom", ex);
            }
            catch (JsonReaderException ex)
            {
                Logger.Error("Exception in reading spdx bom", ex);
            }          

            return bom;
        }
        private static JsonNode GetSpdxDocument(JsonArray graph)
        {
            return graph.FirstOrDefault(e => e?["type"]?.ToString() == "SpdxDocument");
        }
        private static Dictionary<string, string> BuildPackageMap(JsonArray graph)
        {
            try
            {
                return graph
                    .Where(e => e?["type"]?.ToString() == "software_Package")
                    .ToDictionary(
                        e => e?["spdxId"]?.ToString() ?? "",
                        e => e?["software_packageUrl"]?.ToString()
                    );
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to build the package map.", ex);
                throw new InvalidOperationException("Error occurred while building the package map.", ex);
            }
        }
        private static Bom InitializeBom()
        {
            return new Bom
            {
                Components = new List<Component>(),
                Dependencies = new List<Dependency>()
            };
        }


        private static bool IsValidSpecVersion(JsonArray graph, JsonNode spdxDoc)
        {
            var creationInfoId = spdxDoc["creationInfo"]?.ToString();
            var creationInfo = graph.FirstOrDefault(e => e?["@id"]?.ToString() == creationInfoId && e?["type"]?.ToString() == "CreationInfo");
            var specVersion = creationInfo?["specVersion"]?.ToString();

            if (string.IsNullOrWhiteSpace(specVersion))
            {
                Logger.Warn("SPDX specVersion is missing.");
                return false;
            }

            try
            {
                var parsedVersion = Version.Parse(specVersion);
                var minimumSupportedVersion = new Version(3, 0);

                if (parsedVersion < minimumSupportedVersion)
                {
                    Logger.Warn($"Unsupported SPDX version detected: {specVersion}");
                    return false;
                }
            }
            catch (FormatException)
            {
                Logger.Warn($"Invalid SPDX version format: {specVersion}");
                return false;
            }

            return true;
        }

        private static void BuildComponents(JsonArray graph, Bom bom)
        {
            foreach (var pkg in graph.Where(e => e?["type"]?.ToString() == "software_Package"))
            {
                try
                {
                    string name = pkg["name"]?.ToString();
                    string version = pkg["software_packageVersion"]?.ToString();
                    string purl = pkg["software_packageUrl"]?.ToString();

                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(purl))
                    {
                        Logger.Debug($"Skipping component creation due to missing data: Name={name}, Version={version}, Purl={purl}");
                        continue;
                    }

                    var component = new Component
                    {
                        Type = Component.Classification.Library,
                        Name = name,
                        Version = version,
                        BomRef = purl,
                        Purl = purl
                    };

                    bom.Components.Add(component);
                }
                catch (ArgumentNullException ex)
                {
                    Logger.Error("A required argument was null while processing a software package.", ex);
                }
                catch (InvalidOperationException ex)
                {
                    Logger.Error("An invalid operation occurred while processing a software package.", ex);
                }
                catch (Exception ex) when (ex is FormatException || ex is JsonException)
                {
                    Logger.Error("Failed to process a software package due to invalid data format or JSON parsing error.", ex);
                }

            }
        }

        private static void BuildDependencies(JsonArray graph, Dictionary<string, string> packageMap, Bom bom)
        {
            foreach (var rel in graph)
            {
                try
                {
                    string relationshipType = rel["relationshipType"]?.ToString();
                    string from = rel["from"]?.ToString();
                    var toList = rel["to"]?.AsArray();

                    if (from == null || toList == null || relationshipType != "dependsOn")
                        continue;

                    if (!packageMap.TryGetValue(from, out var fromUrl) || string.IsNullOrWhiteSpace(fromUrl))
                        continue;

                    var toDeps = toList
                        .Select(to => to.ToString())
                        .Where(toId => packageMap.ContainsKey(toId) && !string.IsNullOrWhiteSpace(packageMap[toId]))
                        .Select(toId => new Dependency { Ref = packageMap[toId] })
                        .ToList();

                    if (toDeps.Count != 0)
                    {
                        var dependency = new Dependency
                        {
                            Ref = fromUrl,
                            Dependencies = toDeps
                        };

                        bom.Dependencies.Add(dependency);
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Logger.Error("A required argument was null while processing a dependency relationship.", ex);
                }
                catch (InvalidOperationException ex)
                {
                    Logger.Error("An invalid operation occurred while processing a dependency relationship.", ex);
                }
                catch (Exception ex) when (ex is FormatException || ex is JsonException)
                {
                    Logger.Error("Failed to process a dependency relationship due to invalid data format or JSON parsing error.", ex);
                }
            }

        }
    }
}