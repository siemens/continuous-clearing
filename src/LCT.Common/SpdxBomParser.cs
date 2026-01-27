// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

// Ignore Spelling: LCT Spdx Bom

using CycloneDX.Models;
using LCT.Common.Interface;
using LCT.Common.Model;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LCT.Common
{
    public class SpdxBomParser : ISpdxBomParser
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public Bom ParseSPDXBom(string filePath)
        {
            Logger.DebugFormat("ParseSPDXBom():Starting SPDX BOM parsing for file: {0}", filePath);
            Bom bom = new Bom();
            bom.Components = new List<Component>();
            bom.Dependencies = new List<Dependency>();
            SpdxBomData spdxBomData;
            string json;

            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    json = System.IO.File.ReadAllText(filePath);
                    spdxBomData = JsonConvert.DeserializeObject<SpdxBomData>(json);
                    if (!IsValidSpdxVersion(spdxBomData))
                    {
                        Logger.Warn($"    Invalid SPDX version found in this file path {filePath}. Expected 'SPDX-2.3', but found '{spdxBomData?.SpdxVersion}'. Only SPDX version 2.3 is supported.");
                        return bom; // Return empty BOM
                    }
                    // Convert SpdxBomData to Bom here
                    ConvertSpdxDataToBom(spdxBomData, ref bom);
                }
                else
                {
                    Logger.Error($"File not found: {filePath}. Please provide a valid file path.");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Unauthorized access while reading the Spdx BOM file.", "ParseSPDXBom()", ex, $"File Path: {filePath}");
                Logger.Error("Unauthorized access exception in reading SPDX BOM", ex);
            }
            catch (FileNotFoundException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("File not found while reading the Spdx BOM file.", "ParseSPDXBom()", ex, $"File Path: {filePath}");
                Logger.Error("File not found exception in reading SPDX BOM", ex);
            }
            catch (JsonReaderException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Error occurred while reading the Spdx BOM file.", "ParseSPDXBom()", ex, $"File Path: {filePath}");
                Logger.Error("JSON reader exception in reading SPDX BOM", ex);
            }
            Logger.Debug($"SPDX BOM parsing completed. Final BOM contains {bom.Components?.Count ?? 0} components and {bom.Dependencies?.Count ?? 0} dependencies");
            return bom;
        }

        /// <summary>
        /// Validates the SPDX version to ensure it matches the expected version.
        /// </summary>
        /// <param name="spdxData">The SPDX BOM data to validate.</param>
        /// <returns>True if the SPDX version is valid; otherwise, false.</returns>
        private static bool IsValidSpdxVersion(SpdxBomData spdxData)
        {
            if (spdxData == null)
            {
                Logger.Debug("SPDX data is null");
                return false;
            }

            const string EXPECTED_SPDX_VERSION = "SPDX-2.3";

            if (string.IsNullOrEmpty(spdxData.SpdxVersion))
            {
                Logger.Debug("SPDX version is null or empty");
                return false;
            }

            bool isValid = spdxData.SpdxVersion.Equals(EXPECTED_SPDX_VERSION, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                Logger.Debug($"SPDX version validation failed. Expected: '{EXPECTED_SPDX_VERSION}', Found: '{spdxData.SpdxVersion}'");
            }
            return isValid;
        }

        /// <summary>
        /// Converts SPDX data to BOM format including components and dependencies.
        /// </summary>
        /// <param name="spdxData">The SPDX BOM data to convert.</param>
        /// <param name="bom">The BOM object to populate.</param>
        private static void ConvertSpdxDataToBom(SpdxBomData spdxData, ref Bom bom)
        {
            Logger.Debug("Starting SPDX data to BOM conversion...");
            if (spdxData?.Packages == null)
            {
                Logger.Warn("SPDX data or packages is null. No components to convert.");
                return;
            }

            var (components, componentIndex) = ProcessSpdxPackages(spdxData.Packages);
            var dependencies = ProcessSpdxRelationships(spdxData.Relationships, componentIndex);
            CleanupComponentManufacturerData(components);
            bom.Components = components;
            bom.Dependencies = dependencies;
            Logger.DebugFormat("SBOM conversion completed. Components: {0}, Dependencies: {1}", components.Count, dependencies.Count);
        }

        /// <summary>
        /// Adds development property to components based on SPDX relationships.
        /// </summary>
        /// <param name="components">The list of components to update.</param>
        /// <param name="relationships">The SPDX relationships to analyze.</param>
        /// <param name="componentIndex">The component index for lookups.</param>
        private static void AddDevelopmentPropertyToComponents(List<Component> components, IEnumerable<Relationship> relationships, Dictionary<string, Component> componentIndex)
        {
            var devDependencyBomRefs = relationships
                .Where(rel => rel.RelationshipType.Equals("DEV_DEPENDENCY_OF", StringComparison.OrdinalIgnoreCase))
                .Select(rel =>
                {
                    if (componentIndex.TryGetValue(rel.SpdxElementId, out var component))
                    {
                        return component.BomRef;
                    }
                    return null;
                })
                .Where(bomRef => !string.IsNullOrEmpty(bomRef))
                .ToList();

            foreach (var component in components)
            {
                var isDevDependency = devDependencyBomRefs.Contains(component.BomRef);
                SpdxSbomHelper.AddDevelopmentProperty(component, isDevDependency);
            }
        }

        /// <summary>
        /// Cleans up manufacturer data by setting it to null for all components.
        /// </summary>
        /// <param name="components">The list of components to clean up.</param>
        private static void CleanupComponentManufacturerData(List<Component> components)
        {
            components.ForEach(component => component.Manufacturer = null);
        }

        /// <summary>
        /// Processes SPDX packages and converts them to components with an index.
        /// </summary>
        /// <param name="packages">The SPDX packages to process.</param>
        /// <returns>A tuple containing the list of components and a component index.</returns>
        private static (List<Component> components, Dictionary<string, Component> componentIndex) ProcessSpdxPackages(IEnumerable<Package> packages)
        {
            Logger.Debug("ProcessSpdxPackages():Starting SPDX package processing...");
            var components = new List<Component>();
            var componentIndex = new Dictionary<string, Component>();

            foreach (var package in packages)
            {
                var component = CreateComponentFromPackage(package);
                if (component != null)
                {
                    components.Add(component);
                    componentIndex[package.SPDXID] = component;
                }
            }
            Logger.DebugFormat("ProcessSpdxPackages():Total components identified :{0}.", components.Count);
            Logger.Debug("ProcessSpdxPackages():Package processing completed.");
            return (components, componentIndex);
        }

        /// <summary>
        /// Creates a CycloneDX component from an SPDX package.
        /// </summary>
        /// <param name="package">The SPDX package to convert.</param>
        /// <returns>A Component object if successful; otherwise, null.</returns>
        private static Component CreateComponentFromPackage(Package package)
        {
            
            if (package.ExternalRefs == null)
                return null;

            var purlRef = GetPurlReference(package.ExternalRefs);
            if (purlRef == null)
                return null;

            var purl = purlRef.ReferenceLocator;
            var bomRef = !string.IsNullOrEmpty(purl) ? purl : package.SPDXID;
            
            var component = new Component
            {
                Name = package.Name,
                Version = package.VersionInfo,
                Type = Component.Classification.Library,
                Manufacturer = new OrganizationalEntity()
            };

            if (!string.IsNullOrEmpty(purl))
            {
                component.Purl = purl;
                component.BomRef = purl;
            }

            if (!string.IsNullOrEmpty(bomRef))
            {
                component.Manufacturer.BomRef = bomRef;
            }
            
            return component;
        }

        /// <summary>
        /// Gets the PURL reference from external references.
        /// </summary>
        /// <param name="externalRefs">The external references to search.</param>
        /// <returns>The external reference containing the PURL; otherwise, null.</returns>
        private static ExternalRef GetPurlReference(IEnumerable<ExternalRef> externalRefs)
        {
            return externalRefs.FirstOrDefault(er =>
                er.ReferenceCategory?.Equals("PACKAGE-MANAGER", StringComparison.OrdinalIgnoreCase) == true &&
                er.ReferenceType?.Equals("purl", StringComparison.OrdinalIgnoreCase) == true);
        }

        /// <summary>
        /// Processes SPDX relationships and converts them to CycloneDX dependencies.
        /// </summary>
        /// <param name="relationships">The SPDX relationships to process.</param>
        /// <param name="componentIndex">The component index for lookups.</param>
        /// <returns>A list of CycloneDX dependencies.</returns>
        private static List<Dependency> ProcessSpdxRelationships(IEnumerable<Relationship> relationships, Dictionary<string, Component> componentIndex)
        {
            Logger.Debug("ProcessSpdxRelationships(): Starting SPDX relationships processing...");
            if (relationships == null)
            {
                Logger.Debug("ProcessSpdxRelationships(): No relationships found in SPDX data.");
                return new List<Dependency>();
            }

            var supportedRelationshipTypes = GetSupportedRelationshipTypes();
            var dependencyMap = BuildDependencyMap(relationships, componentIndex, supportedRelationshipTypes);
            AddDevelopmentPropertyToComponents(componentIndex.Values.ToList(), relationships, componentIndex);
            var cycloneDxDependencies = ConvertDependencyMapToCycloneDx(dependencyMap);
            Logger.DebugFormat("ProcessSpdxRelationships(): Converted dependency map to CycloneDX format with {0} dependencies.", cycloneDxDependencies.Count);
            Logger.Debug("ProcessSpdxRelationships(): SPDX relationships processing completed.");
            return cycloneDxDependencies;
        }

        /// <summary>
        /// Gets the set of supported SPDX relationship types.
        /// </summary>
        /// <returns>A HashSet containing supported relationship type names.</returns>
        private static HashSet<string> GetSupportedRelationshipTypes()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "DEPENDENCY_OF",
        "DEV_DEPENDENCY_OF",
        "DEPENDS_ON",
        "RUNTIME_DEPENDENCY_OF"
    };
        }

        /// <summary>
        /// Builds a dependency map from SPDX relationships.
        /// </summary>
        /// <param name="relationships">The SPDX relationships to process.</param>
        /// <param name="componentIndex">The component index for lookups.</param>
        /// <param name="supportedRelationshipTypes">The set of supported relationship types.</param>
        /// <returns>A dictionary mapping component references to their dependencies.</returns>
        private static Dictionary<string, List<(string bomRef, string relationshipType)>> BuildDependencyMap(
            IEnumerable<Relationship> relationships,
            Dictionary<string, Component> componentIndex,
            HashSet<string> supportedRelationshipTypes)
        {
            var dependencyMap = new Dictionary<string, List<(string bomRef, string relationshipType)>>();

            foreach (var relationship in relationships)
            {
                if (!IsValidRelationship(relationship, supportedRelationshipTypes, componentIndex))
                    continue;

                var (dependentRef, dependencyRef) = GetDependencyRefs(relationship, componentIndex);
                if (string.IsNullOrEmpty(dependentRef) || string.IsNullOrEmpty(dependencyRef))
                    continue;
                Logger.DebugFormat("BuildDependencyMap(): Adding relationship to dependency map: DependentRef={0}, DependencyRef={1}, Type={2}", dependentRef, dependencyRef, relationship.RelationshipType);
                AddToDependencyMap(dependencyMap, dependentRef, dependencyRef, relationship.RelationshipType);
            }

            return dependencyMap;
        }

        /// <summary>
        /// Validates whether a relationship is valid and supported.
        /// </summary>
        /// <param name="relationship">The relationship to validate.</param>
        /// <param name="supportedTypes">The set of supported relationship types.</param>
        /// <param name="componentIndex">The component index for lookups.</param>
        /// <returns>True if the relationship is valid; otherwise, false.</returns>
        private static bool IsValidRelationship(Relationship relationship, HashSet<string> supportedTypes, Dictionary<string, Component> componentIndex)
        {
            return supportedTypes.Contains(relationship.RelationshipType) &&
                   componentIndex.ContainsKey(relationship.SpdxElementId) &&
                   componentIndex.ContainsKey(relationship.RelatedSpdxElement);
        }

        /// <summary>
        /// Gets the dependency references from a relationship.
        /// </summary>
        /// <param name="relationship">The relationship to extract references from.</param>
        /// <param name="componentIndex">The component index for lookups.</param>
        /// <returns>A tuple containing the dependent reference and dependency reference.</returns>
        private static (string dependentRef, string dependencyRef) GetDependencyRefs(Relationship relationship, Dictionary<string, Component> componentIndex)
        {
            Logger.DebugFormat("GetDependencyRefs(): Resolving dependency references for relationship: {0} -> {1}", relationship.SpdxElementId, relationship.RelatedSpdxElement);
            var parentComponent = componentIndex[relationship.SpdxElementId];
            var childComponent = componentIndex[relationship.RelatedSpdxElement];

            var parentBomRef = parentComponent.Manufacturer.BomRef;
            var childBomRef = childComponent.Manufacturer.BomRef;
            Logger.DebugFormat("GetDependencyRefs(): Resolved dependency references for relationship: ParentBomRef={0}, ChildBomRef={1}", parentBomRef, childBomRef);
            // For DEPENDENCY_OF, DEV_DEPENDENCY_OF, RUNTIME_DEPENDENCY_OF
            // A DEPENDENCY_OF B means A is a dependency of B
            // So B depends on A, meaning B (child) depends on A (parent)
            return (dependentRef: childBomRef, dependencyRef: parentBomRef);
        }

        /// <summary>
        /// Adds a dependency entry to the dependency map.
        /// </summary>
        /// <param name="dependencyMap">The dependency map to update.</param>
        /// <param name="dependentRef">The dependent component reference.</param>
        /// <param name="dependencyRef">The dependency component reference.</param>
        /// <param name="relationshipType">The type of relationship.</param>
        private static void AddToDependencyMap(
            Dictionary<string, List<(string bomRef, string relationshipType)>> dependencyMap,
            string dependentRef,
            string dependencyRef,
            string relationshipType)
        {
            if (!dependencyMap.TryGetValue(dependentRef, out var dependencies))
            {
                dependencies = new List<(string bomRef, string relationshipType)>();
                dependencyMap[dependentRef] = dependencies;
                Logger.DebugFormat("AddToDependencyMap(): Created new dependency list for DependentRef={0}", dependentRef);
            }

            if (!dependencies.Any(d => d.bomRef == dependencyRef))
            {
                dependencies.Add((dependencyRef, relationshipType));
            }
        }

        /// <summary>
        /// Converts the dependency map to CycloneDX dependency format.
        /// </summary>
        /// <param name="dependencyMap">The dependency map to convert.</param>
        /// <returns>A list of CycloneDX dependencies.</returns>
        private static List<Dependency> ConvertDependencyMapToCycloneDx(Dictionary<string, List<(string bomRef, string relationshipType)>> dependencyMap)
        {
            return [.. dependencyMap.Select(kvp => new Dependency
            {
                Ref = kvp.Key,
                Dependencies = [.. kvp.Value.Select(dep => new Dependency { Ref = dep.bomRef })]
            })];
        }

        #endregion
    }
}
