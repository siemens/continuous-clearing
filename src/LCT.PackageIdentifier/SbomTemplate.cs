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
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void ProcessTemplateFile(string templateFilePath, ICycloneDXBomParser cycloneDXBomParser, List<Component> componentsForBOM, string projectType)
        {
            if (File.Exists(templateFilePath) && templateFilePath.EndsWith(FileConstant.SBOMTemplateFileExtension))
            {
                Bom templateDetails = CycloneDXBomParser.ExtractSBOMDetailsFromTemplate(cycloneDXBomParser.ParseCycloneDXBom(templateFilePath));
                CycloneDXBomParser.CheckValidComponentsForProjectType(templateDetails.Components, projectType);
                AddComponentDetails(componentsForBOM, templateDetails);
            }
        }
        public static string GetFilePathForTemplate(List<string> filePaths)
        {
            string firstFilePath = string.Empty;
            if (filePaths != null && filePaths.Count != 0)
            {
                firstFilePath = filePaths.First();
                if (filePaths.Count > 1)
                {
                    Logger.Logger.Log(null, Level.Alert, "Multiple Template files are given", null);
                }
            }
            return firstFilePath;
        }
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
                Logger.Error($"PropertyAdditionForTemplate():ArgumentException:Error from {sbomcomp.Name} : {sbomcomp.Version}", ex);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error($"PropertyAdditionForTemplate():InvalidOperationException:Error from {sbomcomp.Name} : {sbomcomp.Version}", ex);
            }
        }

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
                Logger.Debug($"TemplateComponentUpdation():No Details updated for SBOM Template component " + sbomcomp.Name + " : " + sbomcomp.Version);
            }
        }

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
    }
}
