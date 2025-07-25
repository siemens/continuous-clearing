// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

// Ignore Spelling: LCT Spdx Bom

using CycloneDX.Json;
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
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public  Bom ParseSPDXBom(string filePath)
        {
            Logger.Debug($"Starting SPDX BOM parsing for file: {filePath}");
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
                Logger.Error("Unauthorized access exception in reading SPDX BOM", ex);
            }
            catch (FileNotFoundException ex)
            {
                Logger.Error("File not found exception in reading SPDX BOM", ex);
            }
            catch (JsonReaderException ex)
            {
                Logger.Error("JSON reader exception in reading SPDX BOM", ex);
            }
            Logger.Debug($"SPDX BOM parsing completed. Final BOM contains {bom.Components?.Count ?? 0} components and {bom.Dependencies?.Count ?? 0} dependencies");
            return bom;
        }
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
            Logger.Debug($"BOM conversion completed. Components: {components.Count}, Dependencies: {dependencies.Count}");
        }
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
        private static void CleanupComponentManufacturerData(List<Component> components)
        {
            components.ForEach(component => component.Manufacturer = null);
        }

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
                    Logger.Debug($"Successfully created component for package: {package.Name}");
                }
            }
            Logger.Debug($"ProcessSpdxPackages():Package processing completed.");
            return (components, componentIndex);
        }

        private static Component CreateComponentFromPackage(Package package)
        {
            Logger.Debug($"CreateComponentFromPackage():Creating component from package: {package.Name}");
            if (package.ExternalRefs == null)
                return null;

            var purlRef = GetPurlReference(package.ExternalRefs);
            if (purlRef == null)
                return null;

            var purl = purlRef.ReferenceLocator;
            var bomRef = !string.IsNullOrEmpty(purl) ? purl : package.SPDXID;
            Logger.Debug($"Creating component with PURL: {purl}, BOM Ref: {bomRef}");
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
            Logger.Debug($"CreateComponentFromPackage():Component created successfully for package: {package.Name}");
            return component;
        }

        private static ExternalRef GetPurlReference(IEnumerable<ExternalRef> externalRefs)
        {
            return externalRefs.FirstOrDefault(er =>
                er.ReferenceCategory?.Equals("PACKAGE-MANAGER", StringComparison.OrdinalIgnoreCase) == true &&
                er.ReferenceType?.Equals("purl", StringComparison.OrdinalIgnoreCase) == true);
        }

        private static List<Dependency> ProcessSpdxRelationships(IEnumerable<Relationship> relationships, Dictionary<string, Component> componentIndex)
        {
            if (relationships == null)
            {
                Logger.Debug("No relationships found in SPDX data.");
                return new List<Dependency>();
            }

            var supportedRelationshipTypes = GetSupportedRelationshipTypes();
            var dependencyMap = BuildDependencyMap(relationships, componentIndex, supportedRelationshipTypes);
            AddDevelopmentPropertyToComponents(componentIndex.Values.ToList(), relationships, componentIndex);
            return ConvertDependencyMapToCycloneDx(dependencyMap);
        }

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

                AddToDependencyMap(dependencyMap, dependentRef, dependencyRef, relationship.RelationshipType);
            }

            return dependencyMap;
        }

        private static bool IsValidRelationship(Relationship relationship, HashSet<string> supportedTypes, Dictionary<string, Component> componentIndex)
        {
            return supportedTypes.Contains(relationship.RelationshipType) &&
                   componentIndex.ContainsKey(relationship.SpdxElementId) &&
                   componentIndex.ContainsKey(relationship.RelatedSpdxElement);
        }

        private static (string dependentRef, string dependencyRef) GetDependencyRefs(Relationship relationship, Dictionary<string, Component> componentIndex)
        {
            var parentComponent = componentIndex[relationship.SpdxElementId];
            var childComponent = componentIndex[relationship.RelatedSpdxElement];

            var parentBomRef = parentComponent.Manufacturer.BomRef;
            var childBomRef = childComponent.Manufacturer.BomRef;

            // For DEPENDENCY_OF, DEV_DEPENDENCY_OF, RUNTIME_DEPENDENCY_OF
            // A DEPENDENCY_OF B means A is a dependency of B
            // So B depends on A, meaning B (child) depends on A (parent)
            return (dependentRef: childBomRef, dependencyRef: parentBomRef);
        }

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
            }

            if (!dependencies.Any(d => d.bomRef == dependencyRef))
            {
                dependencies.Add((dependencyRef, relationshipType));
            }
        }

        private static List<Dependency> ConvertDependencyMapToCycloneDx(Dictionary<string, List<(string bomRef, string relationshipType)>> dependencyMap)
        {
            return [.. dependencyMap.Select(kvp => new Dependency
            {
                Ref = kvp.Key,
                Dependencies = [.. kvp.Value.Select(dep => new Dependency { Ref = dep.bomRef })]
            })];
        }       

    }
}
