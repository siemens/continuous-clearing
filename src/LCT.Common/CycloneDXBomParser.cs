// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Json;
using CycloneDX.Models;
using LCT.Common.Constants;
using LCT.Common.Logging;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LCT.Common
{
    public class CycloneDXBomParser : ICycloneDXBomParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public Bom ParseCycloneDXBom(string filePath)
        {
            Logger.Debug("ParseCycloneDXBom():Parseing CycloneDX Bom File started");
            Bom bom = new Bom();
            string json = string.Empty;
            try
            {
                json = File.ReadAllText(filePath);
                bom = JsonConvert.DeserializeObject<Bom>(json);
            }
            catch (JsonSerializationException)
            {
                bom = Serializer.Deserialize(json);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHandling.HttpErrorHandelingForLog("Unauthorized access while reading the CycloneDX BOM file.", "ParseCycloneDXBom()", ex, $"File Path: {filePath}");
                Logger.Error("Exception in reading cycloneDx bom", ex);
            }
            catch (FileNotFoundException ex)
            {
                LogHandling.HttpErrorHandelingForLog("File not found while reading the CycloneDX BOM file.", "ParseCycloneDXBom()", ex, $"File Path: {filePath}");
                Logger.Error("Exception in reading cycloneDx bom", ex);
            }
            catch (JsonReaderException ex)
            {
                LogHandling.HttpErrorHandelingForLog("Error occurred while reading the CycloneDX BOM file.", "ParseCycloneDXBom()", ex, $"File Path: {filePath}");
                Logger.Error("Exception in reading cycloneDx bom", ex);
            }
            Logger.Debug("ParseCycloneDXBom():Parseing CycloneDX Bom File completed\n");
            return bom;
        }

        public static Bom ExtractSBOMDetailsFromTemplate(Bom template)
        {
            Bom bom = new Bom();
            bom.Components = new List<Component>();
            if (template?.Components == null)
            {
                return bom;
            }
            foreach (var component in template.Components)
            {
                if (!string.IsNullOrEmpty(component.Name) && !string.IsNullOrEmpty(component.Version)
                    && !string.IsNullOrEmpty(component.Purl))
                {
                    //Taking SBOM Template Components

                    bom.Components.Add(component);
                }
            }

            //Taking SBOM Template Metadata
            bom.Metadata = template.Metadata;
            bom.Dependencies = template.Dependencies;
            return bom;
        }

        public static void CheckValidComponentsForProjectType(List<Component> bom, string projectType)
        {
            foreach (var component in bom.ToList())
            {
                if (!string.IsNullOrEmpty(component.Name) && !string.IsNullOrEmpty(component.Version)
                    && !string.IsNullOrEmpty(component.Purl) &&
                    component.Purl.Contains(Dataconstant.PurlCheck()[projectType.ToUpper()]))
                {
                    //Taking Valid Components for perticular projects
                }
                else
                {
                    bom.Remove(component);
                    Logger.Debug("CheckValidComponenstForProjectType(): " +
                        "Not valid Component / Purl ID " + component.Purl + " for Project Type :" + projectType);
                }
            }
        }

    }
}
