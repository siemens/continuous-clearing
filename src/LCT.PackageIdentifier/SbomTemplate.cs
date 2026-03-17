// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Constants;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Level = log4net.Core.Level;

namespace LCT.PackageIdentifier
{
    public static class SbomTemplate
    {
        #region Fields
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        #region Properties
        #endregion

        #region Constructors
        #endregion

        #region Methods
        /// <summary>
        /// Processes a SBOM template file and applies template component details to the provided BOM components list.
        /// </summary>
        /// <param name="templateFilePath">Path to the CycloneDX template file.</param>
        /// <param name="cycloneDXBomParser">Parser used to read CycloneDX BOM files.</param>
        /// <param name="componentsForBOM">Component list to which template details will be applied or appended.</param>
        /// <param name="projectType">Project type used to validate components from the template.</param>
        public static void ProcessTemplateFile(string templateFilePath, ICycloneDXBomParser cycloneDXBomParser, List<Component> componentsForBOM, string projectType)
        {
            if (File.Exists(templateFilePath) && templateFilePath.EndsWith(FileConstant.SBOMTemplateFileExtension))
            {
                Bom templateDetails = CycloneDXBomParser.ExtractSBOMDetailsFromTemplate(cycloneDXBomParser.ParseCycloneDXBom(templateFilePath));
                LogHandlingHelper.ComponentsList(templateFilePath, templateDetails.Components);
                CycloneDXBomParser.CheckValidComponentsForProjectType(templateDetails.Components, projectType);
                AddComponentDetails(componentsForBOM, templateDetails);
            }
        }

        /// <summary>
        /// Selects the first template file path from a list and logs when multiple templates are provided.
        /// </summary>
        /// <param name="filePaths">List of candidate template file paths.</param>
        /// <returns>The selected template file path, or an empty string when none provided.</returns>
        public static string GetFilePathForTemplate(List<string> filePaths)
        {
            string firstFilePath = string.Empty;
            if (filePaths != null && filePaths.Count != 0)
            {
                firstFilePath = filePaths[0];
                if (filePaths.Count > 1)
                {
                    Logger.Logger.Log(null, Level.Alert, "Multiple Template files are given", null);
                }
            }
            return firstFilePath;
        }

        /// <summary>
        /// Adds components from the SBOM template into the target BOM list when missing.
        /// </summary>
        /// <param name="bom">Target BOM component list to merge into.</param>
        /// <param name="sbomdDetails">Template BOM details containing components to apply.</param>
        public static void AddComponentDetails(List<Component> bom, Bom sbomdDetails)
        {
            if (sbomdDetails.Components == null)
            {
                return;
            }

            foreach (var sbomcomp in sbomdDetails.Components)
            {
                PropertyAdditionForTemplate(bom, sbomcomp);
            }
        }

        /// <summary>
        /// Adds a template component to the BOM or updates an existing BOM component with template properties/licenses.
        /// </summary>
        /// <param name="bom">Target BOM component list.</param>
        /// <param name="sbomcomp">Template component to add or apply.</param>
        private static void PropertyAdditionForTemplate(List<Component> bom, Component sbomcomp)
        {
            try
            {
                Component bomComp = bom.Find(x => x.Name == sbomcomp.Name && x.Version == sbomcomp.Version);
                if (bomComp == null)
                {
                    sbomcomp.Properties ??= new List<Property>();
                    var properties = sbomcomp.Properties;
                    CommonHelper.RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_IdentifierType, Dataconstant.TemplateAdded);
                    CommonHelper.RemoveDuplicateAndAddProperty(ref properties, Dataconstant.Cdx_IsDevelopment, "false");
                    sbomcomp.Properties = properties;
                    bom.Add(sbomcomp);
                    BomCreator.bomKpiData.ComponentsinSBOMTemplateFile++;
                }
                else
                {
                    TemplateComponentUpdation(sbomcomp, bomComp);
                }
            }
            catch (ArgumentException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("ArgumentException", "PropertyAdditionForTemplate()", ex, $"Component: {sbomcomp.Name} @ {sbomcomp.Version}");
                Logger.Error(string.Format("ArgumentException occurred while adding properties for component: {0} @ {1}. Details: {2}", sbomcomp.Name, sbomcomp.Version, ex.Message), ex);
            }
            catch (InvalidOperationException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("InvalidOperationException", "PropertyAdditionForTemplate()", ex, $"Component: {sbomcomp.Name} @ {sbomcomp.Version}");
                Logger.Error(string.Format("InvalidOperationException occurred while adding properties for component: {0} @ {1}. Details: {2}", sbomcomp.Name, sbomcomp.Version, ex.Message), ex);
            }
        }

        /// <summary>
        /// Updates an existing BOM component using values from a template component and increments KPI counters when updated.
        /// </summary>
        /// <param name="sbomcomp">Template component.</param>
        /// <param name="bomComp">Existing BOM component to update.</param>
        private static void TemplateComponentUpdation(Component sbomcomp, Component bomComp)
        {
            bool isLicenseUpdated = UpdateLicenseDetails(bomComp, sbomcomp);
            bool isPropertiesUpdated = UpdatePropertiesDetails(bomComp, sbomcomp);

            if (isLicenseUpdated || isPropertiesUpdated)
            {

                BomCreator.bomKpiData.ComponentsUpdatedFromSBOMTemplateFile++;
            }
            else
            {
                Logger.DebugFormat("TemplateComponentUpdation():No Details updated for SBOM Template component {0} : {1}",
                    sbomcomp.Name, sbomcomp.Version);
            }
        }

        /// <summary>
        /// Merges license information from the template component into the BOM component when present.
        /// </summary>
        /// <param name="bomComp">Existing BOM component to update.</param>
        /// <param name="sbomcomp">Template component containing license data.</param>
        /// <returns>True if licenses were added; otherwise false.</returns>
        private static bool UpdateLicenseDetails(Component bomComp, Component sbomcomp)
        {
            //Adding Licenses if mainatined
            if (sbomcomp.Licenses?.Count > 0)
            {
                if (bomComp.Licenses != null)
                {
                    bomComp.Licenses.AddRange(sbomcomp.Licenses);
                    return true;
                }
                else
                {
                    bomComp.Licenses = sbomcomp.Licenses;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Merges properties from the template component into the BOM component and sets identifier type to 'TemplateUpdated'.
        /// </summary>
        /// <param name="bomComp">Existing BOM component to update.</param>
        /// <param name="sbomcomp">Template component containing properties.</param>
        /// <returns>True if properties were added; otherwise false.</returns>
        private static bool UpdatePropertiesDetails(Component bomComp, Component sbomcomp)
        {
            //Adding properties if mainatined
            if (sbomcomp.Properties?.Count > 0)
            {
                if (CommonHelper.ComponentPropertyCheck(bomComp, Dataconstant.Cdx_IdentifierType))
                {
                    var val = bomComp.Properties.Single(x => x.Name == Dataconstant.Cdx_IdentifierType);
                    val.Value = Dataconstant.TemplateUpdated;
                }
                else
                {
                    bomComp.Properties.Add(new Property()
                    {
                        Name = Dataconstant.Cdx_IdentifierType,
                        Value = Dataconstant.TemplateUpdated
                    });
                }

                bomComp.Properties?.AddRange(sbomcomp.Properties);
                return true;
            }
            return false;
        }
        #endregion

        #region Events
        #endregion
    }
}
