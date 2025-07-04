// Ignore Spelling: LCT Spdx

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
            Bom bom = new Bom();
            SpdxBomData spdxBomData = null;
            string json = string.Empty;

            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    json = System.IO.File.ReadAllText(filePath);
                    spdxBomData = JsonConvert.DeserializeObject<SpdxBomData>(json);
                    if (!IsValidSpdxVersion(spdxBomData))
                    {
                        Logger.Error($"Invalid SPDX version. Expected 'SPDX-2.3', but found '{spdxBomData?.SpdxVersion}'. Only SPDX version 2.3 is supported.");
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

            return bom;
        }
        private static bool IsValidSpdxVersion(SpdxBomData spdxData)
        {
            if (spdxData == null)
            {
                Logger.Error("SPDX data is null");
                return false;
            }

            const string EXPECTED_SPDX_VERSION = "SPDX-2.3";

            if (string.IsNullOrEmpty(spdxData.SpdxVersion))
            {
                Logger.Error("SPDX version is null or empty");
                return false;
            }

            bool isValid = spdxData.SpdxVersion.Equals(EXPECTED_SPDX_VERSION, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                Logger.Warn($"SPDX version validation failed. Expected: '{EXPECTED_SPDX_VERSION}', Found: '{spdxData.SpdxVersion}'");
            }
            return isValid;
        }
        private static void ConvertSpdxDataToBom(SpdxBomData spdxData, ref Bom bom)
        {
            List<Component> lstComponentForBOM = new List<Component>();
            List<Dependency> lstDependenciesForBOM = new List<Dependency>();
            var compIndex = new Dictionary<string, Component>();
            if (spdxData?.Packages != null)
            {
                foreach (var package in spdxData.Packages)
                {
                    // Check if package has external references with PACKAGE-MANAGER category and purl type
                    if (package.ExternalRefs != null)
                    {
                        var purlRef = package.ExternalRefs.FirstOrDefault(er =>
                            er.ReferenceCategory?.Equals("PACKAGE-MANAGER", StringComparison.OrdinalIgnoreCase) == true &&
                            er.ReferenceType?.Equals("purl", StringComparison.OrdinalIgnoreCase) == true);

                        if (purlRef != null)
                        {
                            var component = new Component
                            {
                                Name = package.Name,
                                Version = package.VersionInfo,
                                Type = Component.Classification.Library

                            };
                            var purl = package.ExternalRefs?.FirstOrDefault(r => r.ReferenceType.Equals("purl", StringComparison.OrdinalIgnoreCase))?.ReferenceLocator;
                            var bomRef=!string.IsNullOrEmpty(purl) ? purl:package.SPDXID;
                            if (!string.IsNullOrEmpty(purl))
                                component.Purl = purl;
                            if (!string.IsNullOrEmpty(bomRef))
                                component.BomRef = bomRef;
                            lstComponentForBOM.Add(component);
                            compIndex[package.SPDXID] = component;
                        }
                    }
                }
            }

            bom.Components = lstComponentForBOM;
            var groupedDependencies = new Dictionary<string, Dependency>();
            foreach (var rel in spdxData.Relationships)
            {
                if (rel.RelationshipType == "DEPENDS_ON" &&
                    compIndex.TryGetValue(rel.SpdxElementId, out var parent) &&
                    compIndex.TryGetValue(rel.RelatedSpdxElement, out var child))
                {
                    var dependency=new Dependency
                    {
                        Ref = parent.BomRef,
                        Dependencies = new List<Dependency> { new Dependency { Ref = child.BomRef } }
                    };
                    lstDependenciesForBOM.Add(dependency);
                }
                
            }
            bom.Dependencies = lstDependenciesForBOM;
            // Log the number of components added for debugging
            Logger.Info($"Converted {lstComponentForBOM.Count} packages to BOM components");
        }
       
    }
}
