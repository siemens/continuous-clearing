// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Json;
using CycloneDX.Models;
using LCT.Common.Constants;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LCT.Common
{
    /// <summary>
    /// Provides parsing functionality for CycloneDX BOM files.
    /// </summary>
    public class CycloneDXBomParser : ICycloneDXBomParser
    {
        #region Fields

        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Methods

        /// <summary>
        /// Parses a CycloneDX BOM file and returns the BOM object.
        /// </summary>
        /// <param name="filePath">The file path of the CycloneDX BOM file.</param>
        /// <returns>A BOM object containing the parsed data.</returns>
        public Bom ParseCycloneDXBom(string filePath)
        {
            Logger.Debug("ParseCycloneDXBom():Parsing CycloneDX Bom File started");
            Bom bom = new Bom();
            string json = string.Empty;
            try
            {
                if (File.Exists(filePath))
                {
                    json = File.ReadAllText(filePath);
                    bom = JsonConvert.DeserializeObject<Bom>(json);
                }
                else
                {
                    Logger.ErrorFormat("File not found: {0}. Please provide a valid file path.", filePath);
                }

            }
            catch (JsonSerializationException)
            {
                bom = Serializer.Deserialize(json);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Unauthorized access while reading the CycloneDX BOM file.", "ParseCycloneDXBom()", ex, $"File Path: {filePath}");
                Logger.Error("Exception in reading cycloneDx bom", ex);
            }
            catch (FileNotFoundException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("File not found while reading the CycloneDX BOM file.", "ParseCycloneDXBom()", ex, $"File Path: {filePath}");
                Logger.Error("Exception in reading cycloneDx bom", ex);
            }
            catch (JsonReaderException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("Error occurred while reading the CycloneDX BOM file.", "ParseCycloneDXBom()", ex, $"File Path: {filePath}");
                Logger.Error("Exception in reading cycloneDx bom", ex);
            }
            Logger.Debug("ParseCycloneDXBom():Parseing CycloneDX Bom File completed\n");
            return bom;
        }

        /// <summary>
        /// Extracts SBOM details from a template BOM, filtering valid components.
        /// </summary>
        /// <param name="template">The template BOM to extract details from.</param>
        /// <returns>A BOM object containing extracted components, metadata, and dependencies.</returns>
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

        /// <summary>
        /// Checks and removes invalid components from the BOM based on project type.
        /// </summary>
        /// <param name="bom">The list of components to validate.</param>
        /// <param name="projectType">The project type to validate against.</param>
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


        public static void CheckValidDependenciesForProjectType(List<Dependency> dependencies, string projectType)
        {
            if (dependencies == null || string.IsNullOrWhiteSpace(projectType))
            {
                return;
            }

            var prefix = Dataconstant.PurlCheck()[projectType.ToUpper()];
            dependencies.RemoveAll(dep => !IsValidDependencyForProjectType(dep, prefix));

            foreach (var childList in dependencies.Select(d => d.Dependencies).Where(list => list != null))
            {
                childList.RemoveAll(child => !IsValidDependencyForProjectType(child, prefix));
            }
        }

        private static bool IsValidDependencyForProjectType(Dependency dep, string requiredPrefix)
        {
            if (dep == null || string.IsNullOrWhiteSpace(dep.Ref)) return false;
            // Require dependency Ref to contain the project-type purl prefix (e.g., "pkg:npm")
            return dep.Ref.Contains(requiredPrefix, StringComparison.Ordinal);
        }

        #endregion

    }
}
