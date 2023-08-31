// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Json;
using CycloneDX.Models;
using LCT.Common.Constants;
using LCT.Common.Model;
using log4net;
using log4net.Core;
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
            Bom bom = new Bom();
            string json = string.Empty;
            Logger.Logger.Log(null, Level.Notice, $"Consuming cyclonedx file data from "+ filePath + "...\n", null);

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
                Logger.Error("Exception in reading cycloneDx bom", ex);
            }
            catch (FileNotFoundException ex)
            {
                Logger.Error("Exception in reading cycloneDx bom", ex);
            }
            catch (JsonReaderException ex)
            {
                Logger.Error("Exception in reading cycloneDx bom", ex);
            }
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
            bom.Metadata = template?.Metadata;
            bom.Dependencies = template?.Dependencies;
            return bom;
        }

        public static void CheckValidComponentsForProjectType(List<Component> bom, string projectType)
        {
            foreach (var component in bom.ToList())
            {
                if (!string.IsNullOrEmpty(component.Name) && !string.IsNullOrEmpty(component.Version)
                    && !string.IsNullOrEmpty(component.Purl) && component.Purl.Contains(Dataconstant.PurlCheck()[projectType.ToUpper()]))
                {
                    //Taking Valid Components for perticular projects
                }
                else
                {
                    bom.Remove(component);
                    Logger.Debug("CheckValidComponenstForProjectType(): Not valid Component / Purl ID " + component.Purl + " for Project Type :" + projectType);
                }
            }
        }
    }
}
